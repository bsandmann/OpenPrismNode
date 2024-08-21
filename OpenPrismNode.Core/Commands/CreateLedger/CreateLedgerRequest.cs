namespace OpenPrismNode.Core.Commands.CreateLedger;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class CreateLedgerRequest : IRequest<Result>
{
    public CreateLedgerRequest(LedgerType ledgerType)
    {
        LedgerType = ledgerType;
    }

    /// <summary>
    /// Leder (testnet, preprod, mainnet)
    /// </summary>
    public LedgerType LedgerType { get; }
}