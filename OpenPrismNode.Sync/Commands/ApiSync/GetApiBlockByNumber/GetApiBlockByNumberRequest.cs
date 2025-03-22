namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockByNumber;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Request to retrieve a block by its number from the Blockfrost API.
/// </summary>
public class GetApiBlockByNumberRequest : IRequest<Result<Block>>
{
    /// <summary>
    /// The block number to retrieve.
    /// </summary>
    public int BlockNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlockByNumberRequest"/> class.
    /// </summary>
    /// <param name="blockNumber">The block number to retrieve</param>
    public GetApiBlockByNumberRequest(int blockNumber)
    {
        BlockNumber = blockNumber;
    }
}