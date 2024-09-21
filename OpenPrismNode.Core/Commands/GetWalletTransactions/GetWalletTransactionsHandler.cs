namespace OpenPrismNode.Core.Commands.GetWalletTransactions;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetWalletTransactionsHandler : IRequestHandler<GetWalletTransactionsRequest, Result<List<GetWalletTransactionsReponse>>>
{
    private readonly DataContext _context;

    public GetWalletTransactionsHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Result<List<GetWalletTransactionsReponse>>> Handle(GetWalletTransactionsRequest request, CancellationToken cancellationToken)
    {
        var walletTransactions = await _context.WalletTransactionEntities
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