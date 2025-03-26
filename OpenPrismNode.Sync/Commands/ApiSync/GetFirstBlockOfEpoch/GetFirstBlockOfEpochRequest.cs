namespace OpenPrismNode.Sync.Commands.ApiSync.GetFirstBlockOfEpoch;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Request to retrieve the first block in a given epoch from the Blockfrost API.
/// </summary>
public class GetFirstBlockOfEpochRequest : IRequest<Result<Block>>
{
    /// <summary>
    /// The epoch number to retrieve.
    /// </summary>
    public int EpochNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetFirstBlockOfEpochRequest"/> class.
    /// </summary>
    /// <param name="epochNumber">The epoch number</param>
    public GetFirstBlockOfEpochRequest(int epochNumber)
    {
        EpochNumber = epochNumber;
    }
}