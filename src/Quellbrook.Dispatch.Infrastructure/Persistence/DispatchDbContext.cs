using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;
using Quellbrook.Dispatch.Infrastructure.Inbox;
using Quellbrook.Dispatch.Infrastructure.Outbox;

namespace Quellbrook.Dispatch.Infrastructure.Persistence;

/// <summary>
/// The dispatch database. Saving also writes one outbox message per domain event raised by the tracked aggregates,
/// in the same transaction (ADR 0003).
/// </summary>
public sealed class DispatchDbContext(DbContextOptions<DispatchDbContext> options) : DbContext(options), IUnitOfWork
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public DbSet<Consignment> Consignments => Set<Consignment>();

    public DbSet<Route> Routes => Set<Route>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>().Select(entry => entry.Entity).Where(entity => entity.DomainEvents.Count > 0).ToList();
        foreach (var domainEvent in aggregates.SelectMany(aggregate => aggregate.DomainEvents))
        {
            var message = DispatchEventMapper.ToIntegrationMessage(domainEvent);
            OutboxMessages.Add(new OutboxMessage
            {
                Id = message.MessageId,
                Type = message.EventType,
                Payload = JsonSerializer.Serialize(message.Payload, message.Payload.GetType(), s_json),
                OccurredAt = domainEvent.OccurredAt,
            });
        }

        var written = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
        return written;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DispatchDbContext).Assembly);
    }
}
