namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionWithPrismMetadataForBlockNo;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetApiTransactionsWithPrismMetadataForBlockNoRequest  : IRequest<Result<List<Transaction>>>
{
    public GetApiTransactionsWithPrismMetadataForBlockNoRequest(int blockNo)
    {
        BlockNo = blockNo;
    }

    public int BlockNo { get; }
}