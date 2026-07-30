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

    private OrderEventsConsumer Consumer() =>
        new(Substitute.For<IRabbitMqConnectionProvider>(), Inbox(), Options.Create(new RabbitMqOptions()), NullLogger<OrderEventsConsumer>.Instance);

    private static BasicDeliverEventArgs Delivery(string? messageId, string body) =>
        new("consumer", 7, false, "quellbrook.events", "orders.order-placed.v1",
            new BasicProperties { MessageId = messageId }, Encoding.UTF8.GetBytes(body));

    public void Dispose() => _fixture.Dispose();
}
