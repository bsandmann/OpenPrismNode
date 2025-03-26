namespace OpenPrismNode.Core.Commands.UpdateOperationStatus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentResults;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using OpenPrismNode.Core.Entities;

    public class UpdateOperationStatusHandler : IRequestHandler<UpdateOperationStatusRequest, Result>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public UpdateOperationStatusHandler(IServiceScopeFactory serviceScopeFactory)
        {
             _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<Result> Handle(UpdateOperationStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                context.ChangeTracker.Clear();
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                var operationStatus = await context.OperationStatusEntities
                    .FirstOrDefaultAsync(os => os.OperationStatusEntityId == request.OperationStatusEntityId, cancellationToken);

                if (operationStatus == null)
                {
                    return Result.Fail("OperationStatus not found.");
                }

                operationStatus.Status = request.Status;
                operationStatus.LastUpdatedUtc = DateTime.UtcNow;

                context.OperationStatusEntities.Update(operationStatus);
                await context.SaveChangesAsync(cancellationToken);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Failed to update operation status").CausedBy(ex));
            }
        }
    }
}