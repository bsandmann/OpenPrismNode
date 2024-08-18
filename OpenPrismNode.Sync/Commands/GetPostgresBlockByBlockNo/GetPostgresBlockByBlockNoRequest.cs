namespace OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockNo;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public class GetPostgresBlockByBlockNoRequest : IRequest<Result<Block>>
{
    public GetPostgresBlockByBlockNoRequest(int blockNo)
    {
        BlockNo = blockNo;
    }

    public int BlockNo { get; }
}