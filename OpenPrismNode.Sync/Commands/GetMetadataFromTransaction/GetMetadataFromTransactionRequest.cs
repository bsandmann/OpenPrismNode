namespace OpenPrismNode.Sync.Commands.GetMetadataFromTransaction;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

// using global::Grpc.Core;

public class GetMetadataFromTransactionRequest : IRequest<Result<Metadata>>
{
    public GetMetadataFromTransactionRequest(int txId, int key)
    {
        TxId = txId;
        Key = key;
    }

    public int TxId { get; }
    public int Key { get; }
}