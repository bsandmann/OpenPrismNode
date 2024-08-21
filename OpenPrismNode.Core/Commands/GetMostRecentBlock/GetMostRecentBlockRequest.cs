namespace OpenPrismNode.Core.Commands.GetMostRecentBlock;

using FluentResults;
using MediatR;
using Models;
using OpenPrismNode.Core.Entities;

public class GetMostRecentBlockRequest : IRequest<Result<BlockEntity>>
{
    public GetMostRecentBlockRequest(LedgerType ledger)
    {
        Ledger = ledger;
    }

    public LedgerType Ledger { get; }
}