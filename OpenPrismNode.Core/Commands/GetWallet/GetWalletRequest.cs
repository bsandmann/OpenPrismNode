namespace OpenPrismNode.Core.Commands.GetWallet;

using FluentResults;
using MediatR;

public class GetWalletRequest : IRequest<Result<GetWalletResponse>>
{
    public GetWalletRequest(string walletId)
    {
        WalletId = walletId;
    }

    public string WalletId { get; }
}