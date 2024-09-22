namespace OpenPrismNode.Core.Commands.CreateWalletTransaction;

using FluentResults;
using MediatR;

public class CreateWalletTransactionEntityRequest : IRequest<Result>
{
    public CreateWalletTransactionEntityRequest(int walletEntityId, string transactionId, int operationStatusEntityId)
    {
        WalletEntityId = walletEntityId;
        TransactionId = transactionId;
        OperationStatusEntityId = operationStatusEntityId;
    }

    public int WalletEntityId { get; }
    public string TransactionId { get; }
    public int OperationStatusEntityId { get; }
}