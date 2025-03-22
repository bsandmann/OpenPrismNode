namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockByBlockNo;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetPostgresBlockByBlockNoRequest : IRequest<Result<Block>>
{
    public GetPostgresBlockByBlockNoRequest(int blockNo)
    {
        BlockNo = blockNo;
    }

    public int BlockNo { get; }
}