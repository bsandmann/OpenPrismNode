namespace OpenPrismNode.Core.Commands.CreateNetwork;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class CreateNetworkRequest : IRequest<Result>
{
    public CreateNetworkRequest(LedgerType ledgerType)
    {
        LedgerType = ledgerType;
    }

    /// <summary>
    /// Leder (testnet, preprod, mainnet)
    /// </summary>
    public LedgerType LedgerType { get; }
}