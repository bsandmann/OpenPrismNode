namespace OpenPrismNode.Sync.Commands.GetMetadataFromPrismTransactions;

using FluentResults;
// using global::Grpc.Core;
using MediatR;
using PostgresModels;

public class GetMetadataOfPrismTransactionRequest : IRequest<Result<Metadata>>
{
    public GetMetadataOfPrismTransactionRequest(long txId, uint key)
    {
        TxId = txId;
        Key = key;
    }

    public long TxId { get; }
    public uint Key { get; }
}