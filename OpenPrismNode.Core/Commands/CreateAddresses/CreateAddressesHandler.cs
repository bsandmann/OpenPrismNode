using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;

namespace OpenPrismNode.Core.Commands.CreateAddresses;

using CreateStakeAddress;
using CreateWalletAddress;

public class CreateAddressesHandler : IRequestHandler<CreateAddressesRequest, Result<List<WalletAddressEntity>>>
{
    private readonly IMediator _mediator;

    public CreateAddressesHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<List<WalletAddressEntity>>> Handle(CreateAddressesRequest request, CancellationToken cancellationToken)
    {
        var uniqueWalletAddresses = request.WalletAddresses
            .Select(wa => wa.WalletAddressString)
            .Distinct()
            .ToList();

        var uniqueStakeAddresses = request.WalletAddresses
            .Select(wa => wa.StakeAddressString!)
            .Select(s => string.IsNullOrWhiteSpace(s) ? "Unknown_Enterprise_Wallet" : s)
            .Distinct()
            .ToList();


        foreach (var uniqueWalletAddress in uniqueWalletAddresses)
        {
            var walletAddressCreateResult = await _mediator.Send(new CreateWalletAddressRequest(uniqueWalletAddress), cancellationToken);
            if (walletAddressCreateResult.IsFailed)
            {
                return Result.Fail($"Failed to create wallet address in the database for {request.WalletAddresses}");
            }
        }

        foreach (var uniqueStakeAddress in uniqueStakeAddresses)
        {
            var stakeAddressCreateResult = await _mediator.Send(new CreateStakeAddressRequest(uniqueStakeAddress), cancellationToken);
            if (stakeAddressCreateResult.IsFailed)
            {
                return Result.Fail($"Failed to create stake address in the database for {uniqueStakeAddress}");
            }
        }

        return Result.Ok();
    }
}