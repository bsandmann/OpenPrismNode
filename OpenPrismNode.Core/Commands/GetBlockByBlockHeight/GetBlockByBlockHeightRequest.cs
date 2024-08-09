namespace OpenPrismNode.Core.Commands.GetBlockByBlockHeight;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

public class GetBlockByBlockHeightRequest : IRequest<Result<BlockEntity>>
{
    public GetBlockByBlockHeightRequest(LedgerType networkType, int blockHeight)
    {
        NetworkType = networkType;
        BlockHeight = blockHeight;
    }

    public LedgerType NetworkType { get; }
    public int BlockHeight { get; }
}