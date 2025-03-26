namespace OpenPrismNode.Core.Commands.CreateWalletTransaction;

using FluentResults;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OpenPrismNode.Core.Entities;

public class CreateWalletTransactionEntityHandler : IRequestHandler<CreateWalletTransactionEntityRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateWalletTransactionEntityHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result> Handle(CreateWalletTransactionEntityRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionId))
        {
            return Result.Fail("TransactionId is required");
        }

        if (request.WalletEntityId == 0)
        {
            return Result.Fail("WalletId is required");
        }

        if (request.OperationStatusEntityId == 0)
        {
            return Result.Fail("OperationStatusEntityId is required");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        await context.WalletTransactionEntities.AddAsync(new WalletTransactionEntity()
        {
            OperationStatusEntityId = request.OperationStatusEntityId,
            // OperationStatusId = request.OperationStatusId,
            TransactionId = request.TransactionId,
            CreatedUtc = DateTime.UtcNow,
            WalletEntityId = request.WalletEntityId,
            Depth = 0,
            LastUpdatedUtc = DateTime.UtcNow
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}