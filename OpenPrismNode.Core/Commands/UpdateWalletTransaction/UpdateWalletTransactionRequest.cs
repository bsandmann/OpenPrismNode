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

    public int WalletTransactionEntityId { get; }

    public long Depth { get; }

    public long Fee { get; }
}