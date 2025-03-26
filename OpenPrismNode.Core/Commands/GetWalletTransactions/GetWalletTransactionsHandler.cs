namespace OpenPrismNode.Core.Commands.GetWalletTransactions;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class GetWalletTransactionsHandler : IRequestHandler<GetWalletTransactionsRequest, Result<List<GetWalletTransactionsReponse>>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetWalletTransactionsHandler(IServiceScopeFactory serviceScopeFactory)
    {
         _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<List<GetWalletTransactionsReponse>>> Handle(GetWalletTransactionsRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var walletTransactions = await context.WalletTransactionEntities
            .Select(p => new GetWalletTransactionsReponse
            {
                WalletEntityId = p.WalletEntityId,
                WalletId = p.Wallet.WalletId,
                WalletTransactionEntityId = p.WalletTransactionEntityId,
                Fee = p.Fee,
                TransactionId = p.TransactionId,
                OperationStatusEntityId = p.OperationStatusEntity != null ? p.OperationStatusEntity.OperationStatusEntityId : null,
                OperationStatusId = p.OperationStatusEntity != null ? p.OperationStatusEntity.OperationStatusId : null,
                OperationHash = p.OperationStatusEntity != null ? p.OperationStatusEntity.OperationHash : null,
                OperationType = p.OperationStatusEntity != null ? p.OperationStatusEntity.OperationType : null,
                Status = p.OperationStatusEntity != null ? p.OperationStatusEntity.Status : null,
            })
            .Where(p => p.WalletId == request.WalletId)
            .ToListAsync(cancellationToken: cancellationToken);

        return Result.Ok(walletTransactions);
    }
}