namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresFirstBlockOfEpoch;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetPostgresFirstBlockOfEpochRequest : IRequest<Result<Block>>
{
    public GetPostgresFirstBlockOfEpochRequest(int epochNumber)
    {
        EpochNumber = epochNumber;
    }

    public int EpochNumber { get; }
}