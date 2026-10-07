using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quellbrook.Dispatch.Infrastructure.Inbox;
using Quellbrook.Dispatch.Infrastructure.Messaging;
using Quellbrook.Dispatch.UnitTests.TestSupport;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Quellbrook.Dispatch.UnitTests.Infrastructure;

public sealed class InboxAndConsumerTests : IDisposable
{
    private const string OrderPlaced = """
        {"orderId":"0198f1a2-0000-7000-8000-000000000042","customerAccountId":"QB-104233","serviceLevel":"express",
         "consignee":{"name":"Halden Bikes ApS","address":{"line1":"Søndergade 12","postalCode":"8000","city":"Aarhus C","countryCode":"DK"},"contact":{}},
         "parcels":[{"number":1,"weightGrams":2400,"lengthCm":40,"widthCm":30,"heightCm":20}],"placedAt":"2026-08-03T06:00:00+00:00"}
        """;

    private readonly DbFixture _fixture = new();

    private InboxProcessor Inbox() =>
        new(_fixture.Services.GetRequiredService<IServiceScopeFactory>(), _fixture.Time, NullLogger<InboxProcessor>.Instance);

    [Fact]
    public async Task AMessageIsProcessedOnceAndItsRedeliveryIsSkipped()
    {
        var messageId = Guid.NewGuid();

        var first = await Inbox().ProcessAsync(messageId, "orders.order-placed.v1", Encoding.UTF8.GetBytes(OrderPlaced), TestContext.Current.CancellationToken);
        var redelivered = await Inbox().ProcessAsync(messageId, "orders.order-placed.v1", Encoding.UTF8.GetBytes(OrderPlaced), TestContext.Current.CancellationToken);

        Assert.Equal(InboxOutcome.Processed, first);
        Assert.Equal(InboxOutcome.Duplicate, redelivered);
        using var context = _fixture.Context();
        var consignment = await context.Consignments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(Guid.Parse("0198f1a2-0000-7000-8000-000000000042"), consignment.OrderId);
        Assert.Equal(Quellbrook.Dispatch.Domain.Consignments.ServiceLevel.Express, consignment.ServiceLevel);
        Assert.Equal(messageId, (await context.InboxMessages.SingleAsync(TestContext.Current.CancellationToken)).MessageId);
    }

    [Fact]
    public async Task ACancelledOrderDropsItsConsignmentFromTheRoute()
    {
        await Inbox().ProcessAsync(Guid.NewGuid(), "orders.order-placed.v1", Encoding.UTF8.GetBytes(OrderPlaced), TestContext.Current.CancellationToken);
        var candidate = DispatchData.Candidate();
        using (var context = _fixture.Context())
        {
            var consignment = await context.Consignments.SingleAsync(TestContext.Current.CancellationToken);
            context.Drivers.Add(candidate.Driver);
            context.Vehicles.Add(candidate.Vehicle);
            context.Routes.Add(candidate.Route);
            candidate.Route.AddStop(consignment);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var cancelled = """{"orderId":"0198f1a2-0000-7000-8000-000000000042","reason":"Shipper withdrew the order","cancelledAt":"2026-08-03T07:00:00+00:00"}""";
        var outcome = await Inbox().ProcessAsync(Guid.NewGuid(), "orders.order-cancelled.v1", Encoding.UTF8.GetBytes(cancelled), TestContext.Current.CancellationToken);

        Assert.Equal(InboxOutcome.Processed, outcome);
        using var reading = _fixture.Context();
        Assert.Equal(Quellbrook.Dispatch.Domain.Consignments.ConsignmentStatus.Cancelled, (await reading.Consignments.SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Empty((await reading.Routes.SingleAsync(TestContext.Current.CancellationToken)).Stops);
    }

    [Fact]
    public async Task AnUnknownEventTypeIsRecordedAndIgnored()
    {
        var outcome = await Inbox().ProcessAsync(Guid.NewGuid(), "orders.order-archived.v1", "{}"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(InboxOutcome.Ignored, outcome);
        using var context = _fixture.Context();
        Assert.Empty(context.Consignments);
    }

    [Fact]
    public async Task AMalformedMessageLeavesNoTrace()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => Inbox().ProcessAsync(Guid.NewGuid(), "orders.order-placed.v1", "not json"u8.ToArray(), TestContext.Current.CancellationToken));

        using var context = _fixture.Context();
        Assert.Empty(context.InboxMessages);
    }

    [Fact]
    public async Task TheConsumerAcknowledgesAProcessedMessage()
    {
        var channel = Substitute.For<IChannel>();

        await Consumer().HandleAsync(channel, Delivery(Guid.NewGuid().ToString(), OrderPlaced), TestContext.Current.CancellationToken);

        await channel.Received(1).BasicAckAsync(7, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheConsumerDeadLettersAMessageWithoutIdOrThatFails()
    {
        var channel = Substitute.For<IChannel>();

        await Consumer().HandleAsync(channel, Delivery(null, OrderPlaced), TestContext.Current.CancellationToken);
        await Consumer().HandleAsync(channel, Delivery(Guid.NewGuid().ToString(), "not json"), TestContext.Current.CancellationToken);

        await channel.Received(2).BasicNackAsync(7, false, false, Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicAckAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheConsumerDeclaresItsDurableQueueWithDeadLetteringBindsBothEventsAndConsumes()
    {
        var channel = Substitute.For<IChannel>();
        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>()).Returns(channel);
        var connections = Substitute.For<IRabbitMqConnectionProvider>();
        connections.GetConnectionAsync(Arg.Any<CancellationToken>()).Returns(connection);
        var consumer = new OrderEventsConsumer(connections, Inbox(), Options.Create(new RabbitMqOptions { Exchange = "quellbrook.events" }),
            NullLogger<OrderEventsConsumer>.Instance);

        var consuming = new TaskCompletionSource();
        channel.BasicConsumeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<IDictionary<string, object?>?>(), Arg.Any<IAsyncBasicConsumer>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                consuming.TrySetResult();
                return Task.FromResult("consumer-tag");
            });

        await consumer.StartAsync(TestContext.Current.CancellationToken);
        await consuming.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await consumer.StopAsync(TestContext.Current.CancellationToken);

        await channel.Received(1).QueueDeclareAsync(
            OrderEventsConsumer.QueueName, Arg.Is(true), Arg.Is(false), Arg.Is(false),
            Arg.Is<IDictionary<string, object?>>(arguments => (string?)arguments["x-dead-letter-exchange"] == OrderEventsConsumer.DeadLetterExchange),
            Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await channel.Received(1).QueueBindAsync(OrderEventsConsumer.QueueName, "quellbrook.events", "orders.order-placed.v1",
            Arg.Any<IDictionary<string, object?>?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await channel.Received(1).QueueBindAsync(OrderEventsConsumer.QueueName, "quellbrook.events", "orders.order-cancelled.v1",
            Arg.Any<IDictionary<string, object?>?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await channel.Received(1).BasicConsumeAsync(OrderEventsConsumer.QueueName, Arg.Is(false), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object?>?>(), Arg.Any<IAsyncBasicConsumer>(), Arg.Any<CancellationToken>());
    }

    private OrderEventsConsumer Consumer() =>
        new(Substitute.For<IRabbitMqConnectionProvider>(), Inbox(), Options.Create(new RabbitMqOptions()), NullLogger<OrderEventsConsumer>.Instance);

    private static BasicDeliverEventArgs Delivery(string? messageId, string body) =>
        new("consumer", 7, false, "quellbrook.events", "orders.order-placed.v1",
            new BasicProperties { MessageId = messageId }, Encoding.UTF8.GetBytes(body));

    public void Dispose() => _fixture.Dispose();
}
