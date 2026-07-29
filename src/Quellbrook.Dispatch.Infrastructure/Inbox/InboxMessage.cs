namespace Quellbrook.Dispatch.Infrastructure.Inbox;

/// <summary>A message already processed, by its AMQP message id (ADR 0003).</summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}
