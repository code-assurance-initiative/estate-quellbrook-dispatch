using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quellbrook.Dispatch.Application.Intake;
using Quellbrook.Dispatch.Infrastructure.Persistence;

namespace Quellbrook.Dispatch.Infrastructure.Inbox;

public enum InboxOutcome
{
    Processed,
    Duplicate,
    Ignored,
}

/// <summary>
/// Runs the handler for one received message exactly once per message id: the inbox row and the handler's changes
/// are committed in one transaction, and a message id already in the inbox is skipped (ADR 0003).
/// </summary>
public sealed partial class InboxProcessor(IServiceScopeFactory scopes, TimeProvider time, ILogger<InboxProcessor> logger)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public async Task<InboxOutcome> ProcessAsync(Guid messageId, string eventType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<DispatchDbContext>();
        if (await db.InboxMessages.AnyAsync(message => message.MessageId == messageId, cancellationToken).ConfigureAwait(false))
        {
            LogDuplicate(messageId, eventType);
            return InboxOutcome.Duplicate;
        }

        var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            db.InboxMessages.Add(new InboxMessage { MessageId = messageId, Type = eventType, ProcessedAt = time.GetUtcNow() });
            var handled = await DispatchAsync(services, eventType, body, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return handled ? InboxOutcome.Processed : InboxOutcome.Ignored;
        }
    }

    private static async Task<bool> DispatchAsync(IServiceProvider services, string eventType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case OrderPlacedMessage.EventType:
                await services.GetRequiredService<OrderPlacedHandler>()
                    .HandleAsync(Deserialize<OrderPlacedMessage>(body), cancellationToken).ConfigureAwait(false);
                return true;
            default:
                return false;
        }
    }

    private static T Deserialize<T>(ReadOnlyMemory<byte> body) =>
        JsonSerializer.Deserialize<T>(body.Span, s_json) ?? throw new JsonException($"Empty {typeof(T).Name} message.");

    [LoggerMessage(Level = LogLevel.Information, Message = "Message {MessageId} ({EventType}) was already processed; skipped")]
    private partial void LogDuplicate(Guid messageId, string eventType);
}
