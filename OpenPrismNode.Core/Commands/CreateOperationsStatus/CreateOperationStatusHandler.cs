using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateOperationsStatus;

public class CreateOperationStatusHandler : IRequestHandler<CreateOperationStatusRequest, Result<byte[]>>
{
    private readonly DataContext _context;

    public CreateOperationStatusHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<byte[]>> Handle(CreateOperationStatusRequest request, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            // Check if OperationStatus already exists
            var existingStatus = await _context.OperationStatusEntities
                .FirstOrDefaultAsync(os => os.OperationStatusId.SequenceEqual(request.OperationStatusId), cancellationToken);

            if (existingStatus != null)
            {
                // Update existing status
                existingStatus.Status = request.Status;
                existingStatus.LastUpdatedUtc = DateTime.UtcNow;
                _context.OperationStatusEntities.Update(existingStatus);
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Ok(existingStatus.OperationStatusId);
            }

            // Create new OperationStatus
            var operationStatus = new OperationStatusEntity
            {
                OperationStatusId = request.OperationStatusId,
                OperationHash = request.OperationHash,
                CreatedUtc = DateTime.UtcNow,
                Status = request.Status,
                OperationType = request.OperationType
            };

            // Try to link to existing entity
            // switch (request.OperationType)
            // {
            //     case OperationTypeEnum.CreateDid:
            //         var createDid = await _context.CreateDidEntities
            //             .FirstOrDefaultAsync(cd => cd.OperationHash.SequenceEqual(request.OperationHash), cancellationToken);
            //         operationStatus.CreateDidEntity = createDid;
            //         break;
            //     case OperationTypeEnum.UpdateDid:
            //         var updateDid = await _context.UpdateDidEntities
            //             .FirstOrDefaultAsync(ud => ud.OperationHash.SequenceEqual(request.OperationHash), cancellationToken);
            //         operationStatus.UpdateDidEntity = updateDid;
            //         break;
            //     case OperationTypeEnum.DeactivateDid:
            //         var deactivateDid = await _context.DeactivateDidEntities
            //             .FirstOrDefaultAsync(dd => dd.OperationHash.SequenceEqual(request.OperationHash), cancellationToken);
            //         operationStatus.DeactivateDidEntity = deactivateDid;
            //         break;
            // }

            await _context.OperationStatusEntities.AddAsync(operationStatus, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok(operationStatus.OperationStatusId);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to create operation status").CausedBy(ex));
        }
    }
}