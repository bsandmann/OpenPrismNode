using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateOperationsStatus;

using Microsoft.Extensions.DependencyInjection;

public class CreateOperationStatusHandler : IRequestHandler<CreateOperationStatusRequest, Result<CreateOperationStatusResponse>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CreateOperationStatusHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<CreateOperationStatusResponse>> Handle(CreateOperationStatusRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            // Check if OperationStatus already exists
            var existingStatus = await context.OperationStatusEntities
                .FirstOrDefaultAsync(os => os.OperationStatusId.SequenceEqual(request.OperationStatusId), cancellationToken);

            if (existingStatus != null)
            {
                return Result.Fail("Operation already exists. Application of the same operation is not allowed.");
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

            await context.OperationStatusEntities.AddAsync(operationStatus, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Ok(new CreateOperationStatusResponse()
            {
                OperationStatusEntityId = operationStatus.OperationStatusEntityId,
                OperationStatusId = operationStatus.OperationStatusId
            });
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to create operation status").CausedBy(ex));
        }
    }
}