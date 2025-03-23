namespace OpenPrismNode.Sync.Commands.DbSync.GetMetadataFromTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;


public class GetMetadataFromTransactionRequest : IRequest<Result<Metadata>>
{
    public GetMetadataFromTransactionRequest(int? txId, byte[]? txHash,  int key)
    {
        TxId = txId;
        TxHash = txHash;
        Key = key;
    }

    public int? TxId { get; }
    public byte[]? TxHash { get; }
    public int Key { get; }

}