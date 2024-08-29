namespace OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockId;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetPostgresBlockByBlockIdRequest : IRequest<Result<Block>>
{
    public GetPostgresBlockByBlockIdRequest(int blockId)
    {
        BlockId = blockId;
    }

    public int BlockId { get; }
}