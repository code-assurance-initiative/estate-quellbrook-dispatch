namespace Quellbrook.Dispatch.Application.Abstractions;

/// <summary>Commits every change of one command, with the outbox messages for the events it raised, atomically.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
