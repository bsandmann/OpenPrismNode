namespace OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public class GetTransactionsWithPrismMetadataForBlockIdRequest : IRequest<Result<List<Transaction>>>
{
    public GetTransactionsWithPrismMetadataForBlockIdRequest(int blockId)
    {
        BlockId = blockId;
    }

    public int BlockId { get; }
}