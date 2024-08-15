using FluentResults;
using MediatR;

namespace OpenPrismNode.Core.Commands.CreateWalletAddress;

public class CreateWalletAddressRequest : IRequest<Result>
{
    public CreateWalletAddressRequest(string walletAddress)
    {
        WalletAddress = walletAddress;
    }

    /// <summary>
    /// Wallet address string
    /// </summary>
    public string WalletAddress { get; }
}