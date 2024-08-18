namespace OpenPrismNode.Sync.Commands.GetPaymentDataFromTransaction;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public class GetPaymentDataFromTransactionRequest : IRequest<Result<Payment>>
{
    public GetPaymentDataFromTransactionRequest(int txId)
    {
        TxId = txId;
    }

    public int TxId { get; }
}