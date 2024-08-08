namespace OpenPrismNode.Sync.Commands.GetMetadataFromTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Sync.PostgresModels;
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