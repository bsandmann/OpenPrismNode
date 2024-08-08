namespace OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;

using FluentResults;
using MediatR;
using OpenPrismNode.Sync.PostgresModels;

public class GetTransactionsWithPrismMetadataForBlockIdRequest : IRequest<Result<List<Transaction>>>
{
    public GetTransactionsWithPrismMetadataForBlockIdRequest(int blockId)
    {
        BlockId = blockId;
    }

    public int BlockId { get; }
}