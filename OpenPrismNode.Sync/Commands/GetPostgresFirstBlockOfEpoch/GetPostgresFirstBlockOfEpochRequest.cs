namespace OpenPrismNode.Sync.Commands.GetPostgresFirstBlockOfEpoch;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public class GetPostgresFirstBlockOfEpochRequest : IRequest<Result<Block>>
{
    public GetPostgresFirstBlockOfEpochRequest(int epochNumber)
    {
        EpochNumber = epochNumber;
    }

    public int EpochNumber { get; }
}