namespace OpenPrismNode.Sync.Commands.DbSync.GetPaymentDataFromTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetPaymentDataFromTransactionRequest : IRequest<Result<Payment>>
{
    public GetPaymentDataFromTransactionRequest(int txId)
    {
        TxId = txId;
    }

    public int TxId { get; }
}