namespace OpenPrismNode.Core.Commands.GetOperationStatus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using OpenPrismNode.Core.Entities;

    public class GetOperationStatusHandler : IRequestHandler<GetOperationStatusRequest, Result<OperationStatusEntity>>
    {
        private readonly DataContext _context;

        public GetOperationStatusHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<Result<OperationStatusEntity>> Handle(GetOperationStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var operationStatus = await _context.OperationStatusEntities
                    .Select(p => new OperationStatusEntity()
                    {
                        OperationStatusId = p.OperationStatusId,
                        Status = p.Status,
                        OperationHash = p.OperationHash,
                        OperationType = p.OperationType,
                        CreatedUtc = p.CreatedUtc,
                        LastUpdatedUtc = p.LastUpdatedUtc,
                        // CreateDidEntity = p.CreateDidEntity != null ? new CreateDidEntity() { OperationHash = p.CreateDidEntity.OperationHash } : null,
                        // UpdateDidEntity = p.UpdateDidEntity != null ? new UpdateDidEntity() { OperationHash = p.UpdateDidEntity.OperationHash } : null,
                        // DeactivateDidEntity = p.DeactivateDidEntity != null ? new DeactivateDidEntity() { OperationHash = p.DeactivateDidEntity.OperationHash } : null,
                    })
                    .FirstOrDefaultAsync(os => os.OperationStatusId == request.OperationStatusId, cancellationToken);

                if (operationStatus == null)
                {
                    return Result.Fail("OperationStatus not found.");
                }

                return Result.Ok(operationStatus);
            }
            catch (Exception ex)
            {
                return Result.Fail("Failed to retrieve operation status");
            }
        }
    }
}