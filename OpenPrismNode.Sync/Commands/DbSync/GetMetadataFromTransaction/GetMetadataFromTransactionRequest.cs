namespace OpenPrismNode.Sync.Commands.DbSync.GetMetadataFromTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetMetadataFromTransactionRequest : IRequest<Result<Metadata>>
{
    public GetMetadataFromTransactionRequest(int? txId, int key)
    {
        TxId = txId;
        Key = key;
    }

    public int? TxId { get; }
    public int Key { get; }
}