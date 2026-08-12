using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quellbrook.Dispatch.Infrastructure.Inbox;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Quellbrook.Dispatch.Infrastructure.Messaging;

/// <summary>
/// Consumes the order service's events from the durable queue <c>dispatch.order-events</c>. A message is acknowledged
/// once the inbox has processed it; one that cannot be processed is rejected to the dead-letter exchange, where the
/// Fleet team's on-call looks at it.
/// </summary>
public sealed partial class OrderEventsConsumer(
    IRabbitMqConnectionProvider connections,
    InboxProcessor inbox,
    IOptions<RabbitMqOptions> options,
    ILogger<OrderEventsConsumer> logger) : BackgroundService
{
    public const string QueueName = "dispatch.order-events";
    public const string DeadLetterExchange = "quellbrook.dead-letter";

    public static readonly IReadOnlyList<string> RoutingKeys = ["orders.order-placed.v1", "orders.order-cancelled.v1"];

    public async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(delivery);
        if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var messageId))
        {
            LogRejected(delivery.RoutingKey, "no message id");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await inbox.ProcessAsync(messageId, delivery.RoutingKey, delivery.Body, cancellationToken).ConfigureAwait(false);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(exception, messageId, delivery.RoutingKey);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connections.GetConnectionAsync(stoppingToken).ConfigureAwait(false);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
        await using (channel.ConfigureAwait(false))
        {
            var exchange = options.Value.Exchange;
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken)
                .ConfigureAwait(false);
            await channel.QueueDeclareAsync(
                QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange },
                cancellationToken: stoppingToken).ConfigureAwait(false);
            foreach (var routingKey in RoutingKeys)
            {
                await channel.QueueBindAsync(QueueName, exchange, routingKey, cancellationToken: stoppingToken).ConfigureAwait(false);
            }

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken).ConfigureAwait(false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, delivery) => HandleAsync(channel, delivery, stoppingToken);
            await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken).ConfigureAwait(false);
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageId} ({RoutingKey}) failed and was dead-lettered")]
    private partial void LogFailed(Exception exception, Guid messageId, string routingKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A {RoutingKey} message was dead-lettered: {Reason}")]
    private partial void LogRejected(string routingKey, string reason);
}
