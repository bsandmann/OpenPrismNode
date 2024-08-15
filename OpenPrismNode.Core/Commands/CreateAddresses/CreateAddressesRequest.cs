namespace OpenPrismNode.Core.Commands.CreateAddresses;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

public class CreateAddressesRequest : IRequest<Result<List<WalletAddressEntity>>>
{
    public CreateAddressesRequest(List<WalletAddress> walletAddresses)
    {
        WalletAddresses = walletAddresses;
    }

    public List<WalletAddress> WalletAddresses { get; }
}