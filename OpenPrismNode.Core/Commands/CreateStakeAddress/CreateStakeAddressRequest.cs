using FluentResults;
using MediatR;

namespace OpenPrismNode.Core.Commands.CreateStakeAddress;

public class CreateStakeAddressRequest : IRequest<Result>
{
    public CreateStakeAddressRequest(string stakeAddress)
    {
        StakeAddress = stakeAddress;
    }

    /// <summary>
    /// Stake address string
    /// </summary>
    public string StakeAddress { get; }
}