namespace OpenPrismNode.Core.Commands.GetBlockByBlockHash;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

public class GetBlockByBlockHashRequest : IRequest<Result<BlockEntity>>
{
    public GetBlockByBlockHashRequest(int blockHeight, int? blockHashPrefix, LedgerType networkType)
    {
        BlockHeight = blockHeight;
        BlockHashPrefix = blockHashPrefix;
        NetworkType = networkType;
    }

    public LedgerType NetworkType { get; }
    public int BlockHeight { get; }
    public int? BlockHashPrefix { get; }
}