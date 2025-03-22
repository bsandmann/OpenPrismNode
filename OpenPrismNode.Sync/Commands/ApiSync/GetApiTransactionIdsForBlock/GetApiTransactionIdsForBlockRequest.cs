namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionIdsForBlock;

using System.Collections.Generic;
using FluentResults;
using MediatR;

/// <summary>
/// Request to retrieve all transaction IDs contained within a specific block
/// </summary>
public class GetApiTransactionIdsForBlockRequest : IRequest<Result<List<string>>>
{
    public GetApiTransactionIdsForBlockRequest(int blockNo)
    {
        BlockNo = blockNo;
    }
    /// <summary>
    /// The block number/height to retrieve transaction IDs from.
    /// </summary>
    public int BlockNo { get;  }
}