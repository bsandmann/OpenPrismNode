namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlocksByNumbers;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;
using System.Collections.Generic;

/// <summary>
/// Request to retrieve <paramref name="Count"/> consecutive blocks from the Blockfrost API,
/// starting at <paramref name="FirstBlockNo"/> (inclusive).
/// </summary>
public class GetApiBlocksByNumbersRequest : IRequest<Result<List<Block>>>
{
    /// <summary>
    /// The starting block number (inclusive).
    /// </summary>
    public int FirstBlockNo { get; }

    /// <summary>
    /// How many consecutive blocks to retrieve (including the first one).
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiBlocksByNumbersRequest"/> class.
    /// </summary>
    /// <param name="firstBlockNo">The block number to start from (inclusive).</param>
    /// <param name="count">The number of consecutive blocks to retrieve.</param>
    public GetApiBlocksByNumbersRequest(int firstBlockNo, int count)
    {
        FirstBlockNo = firstBlockNo;
        Count = count;
    }
}