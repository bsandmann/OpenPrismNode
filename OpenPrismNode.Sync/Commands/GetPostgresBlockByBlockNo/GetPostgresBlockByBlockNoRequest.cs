namespace OpenPrismNode.Sync.Commands.GetPostgresBlockByBlockNo;

using FluentResults;
using MediatR;
using PostgresModels;

public class GetPostgresBlockByBlockNoRequest : IRequest<Result<Block>>
{
    public GetPostgresBlockByBlockNoRequest(int blockNo)
    {
        BlockNo = blockNo;
    }

    public int BlockNo { get; }
}