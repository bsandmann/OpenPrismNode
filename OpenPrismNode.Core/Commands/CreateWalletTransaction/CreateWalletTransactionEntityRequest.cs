namespace OpenPrismNode.Core.Commands.CreateWalletTransaction;

using FluentResults;
using MediatR;

public class CreateWalletTransactionEntityRequest : IRequest<Result>
{
    public int WalletEntityId { get; set; }
    public string TransactionId { get; set; }
    public int OperationStatusEntityId { get; set; }
}