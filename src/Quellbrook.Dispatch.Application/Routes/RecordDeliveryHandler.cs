using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;

namespace Quellbrook.Dispatch.Application.Routes;

public sealed record RecordDeliveryCommand(Guid ConsignmentId, DeliveryProof Proof);

public sealed class RecordDeliveryHandler(IConsignmentRepository consignments, IUnitOfWork unitOfWork, TimeProvider time)
{
    public async Task<OperationResult<ConsignmentId>> HandleAsync(RecordDeliveryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var consignment = await consignments.FindAsync(new ConsignmentId(command.ConsignmentId), cancellationToken).ConfigureAwait(false);
        if (consignment is null)
        {
            return OperationResult.NotFound<ConsignmentId>($"Consignment {command.ConsignmentId} does not exist.");
        }

        try
        {
            consignment.RecordDelivery(command.Proof, time.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OperationResult.Conflict<ConsignmentId>(exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult.Succeeded(consignment.Id);
    }
}
