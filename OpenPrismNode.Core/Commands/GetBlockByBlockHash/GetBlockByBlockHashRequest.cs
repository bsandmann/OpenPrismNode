namespace OpenPrismNode.Core.Commands.GetBlockByBlockHash;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

public class GetBlockByBlockHashRequest : IRequest<Result<BlockEntity>>
{
    public GetBlockByBlockHashRequest(int blockHeight, int? blockHashPrefix, LedgerType ledger)
    {
        BlockHeight = blockHeight;
        BlockHashPrefix = blockHashPrefix;
        Ledger = ledger;
    }

    public LedgerType Ledger { get; }
    public int BlockHeight { get; }
    public int? BlockHashPrefix { get; }
}