namespace OpenPrismNode.Core.Commands.UpdateWalletTransaction;

using FluentResults;
using MediatR;

public class UpdateWalletTransactionRequest : IRequest<Result>
{
    public UpdateWalletTransactionRequest(int walletTransactionEntityId, long depth, long fee)
    {
        WalletTransactionEntityId = walletTransactionEntityId;
        Depth = depth;
        Fee = fee;
    }

    public int WalletTransactionEntityId { get; set; }

    public long Depth { get; set; }

    public long Fee { get; set; }
}