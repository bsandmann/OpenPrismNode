using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

namespace OpenPrismNode.Core.Commands.CreateBlocksAsBatch;

using DbSyncModels;

public class CreateBlocksAsBatchRequest : IRequest<Result<Hash>>
{
    public CreateBlocksAsBatchRequest(LedgerType ledgerType, List<Block> blocks)
    {
        NetworkType = ledgerType;
        Blocks = blocks;
    }

    public LedgerType NetworkType { get; }
    public List<Block> Blocks { get; }
}