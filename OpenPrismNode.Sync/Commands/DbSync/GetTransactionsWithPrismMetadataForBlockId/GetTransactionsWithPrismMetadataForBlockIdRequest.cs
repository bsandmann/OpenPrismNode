namespace OpenPrismNode.Sync.Commands.DbSync.GetTransactionsWithPrismMetadataForBlockId;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetTransactionsWithPrismMetadataForBlockIdRequest : IRequest<Result<List<Transaction>>>
{
    public GetTransactionsWithPrismMetadataForBlockIdRequest(int blockId)
    {
        BlockId = blockId;
    }

    public int BlockId { get; }
}