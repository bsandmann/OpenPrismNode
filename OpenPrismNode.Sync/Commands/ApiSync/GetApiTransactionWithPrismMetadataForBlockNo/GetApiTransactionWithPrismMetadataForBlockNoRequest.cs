namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionWithPrismMetadataForBlockNo;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetApiTransactionsWithPrismMetadataForBlockNoRequest  : IRequest<Result<List<Transaction>>>
{
    public GetApiTransactionsWithPrismMetadataForBlockNoRequest(int blockNo, int currentApiBlockTip)
    {
        BlockNo = blockNo;
        CurrentApiBlockTip = currentApiBlockTip;
    }

    public int BlockNo { get; }
    public int CurrentApiBlockTip { get;  }
}