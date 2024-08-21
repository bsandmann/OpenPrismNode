namespace OpenPrismNode.Core.Commands.GetBlockByBlockHeight;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

public class GetBlockByBlockHeightRequest : IRequest<Result<BlockEntity>>
{
    public GetBlockByBlockHeightRequest(LedgerType ledger, int blockHeight)
    {
        Ledger = ledger;
        BlockHeight = blockHeight;
    }

    public LedgerType Ledger { get; }
    public int BlockHeight { get; }
}