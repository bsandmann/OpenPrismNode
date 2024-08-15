namespace OpenPrismNode.Sync.Commands.GetPostgresFirstBlockOfEpoch;

using FluentResults;
using MediatR;
using PostgresModels;

public class GetPostgresFirstBlockOfEpochRequest : IRequest<Result<Block>>
{
    public GetPostgresFirstBlockOfEpochRequest(int epochNumber)
    {
        EpochNumber = epochNumber;
    }

    public int EpochNumber { get; }
}