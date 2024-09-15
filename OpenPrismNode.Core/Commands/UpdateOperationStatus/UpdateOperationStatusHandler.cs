namespace OpenPrismNode.Core.Commands.UpdateOperationStatus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using OpenPrismNode.Core.Entities;

    public class UpdateOperationStatusHandler : IRequestHandler<UpdateOperationStatusRequest, Result>
    {
        private readonly DataContext _context;

        public UpdateOperationStatusHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(UpdateOperationStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var operationStatus = await _context.OperationStatusEntities
                    .FirstOrDefaultAsync(os => os.OperationStatusId == request.OperationStatusId, cancellationToken);

                if (operationStatus == null)
                {
                    return Result.Fail("OperationStatus not found.");
                }

                operationStatus.Status = request.Status;
                operationStatus.LastUpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Failed to update operation status").CausedBy(ex));
            }
        }
    }
}