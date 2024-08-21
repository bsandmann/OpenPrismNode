using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

namespace OpenPrismNode.Core.Commands.CreateBlocksAsBatch;

using DbSyncModels;

public class CreateBlocksAsBatchRequest : IRequest<Result<Hash>>
{
    public CreateBlocksAsBatchRequest(LedgerType ledgerType, List<Block> blocks)
    {
        ledger = ledgerType;
        Blocks = blocks;
    }

    public LedgerType ledger { get; }
    public List<Block> Blocks { get; }
}