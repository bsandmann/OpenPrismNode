namespace OpenPrismNode.Core.Commands.DeleteLedger;

using FluentResults;
using MediatR;
using Models;

public class DeleteLedgerRequest : IRequest<Result>
{
    public DeleteLedgerRequest(LedgerType ledgerType)
    {
        LedgerType = ledgerType;
    }

    /// <summary>
    /// Leder (testnet, preprod, mainnet)
    /// </summary>
    public LedgerType LedgerType { get; }
}