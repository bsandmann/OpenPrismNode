namespace OpenPrismNode.Sync.Commands.GetPaymentDataFromTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Sync.PostgresModels;

public class GetPaymentDataFromTransactionRequest : IRequest<Result<Payment>>
{
    public GetPaymentDataFromTransactionRequest(int txId)
    {
        TxId = txId;
    }

    public int TxId { get; }
}