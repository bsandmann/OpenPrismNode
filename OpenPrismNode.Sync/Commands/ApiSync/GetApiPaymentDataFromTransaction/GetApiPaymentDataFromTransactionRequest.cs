namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiPaymentDataFromTransaction;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public class GetApiPaymentDataFromTransactionRequest : IRequest<Result<Payment>>
{
    public GetApiPaymentDataFromTransactionRequest(string txHash)
    {
        TxHash = txHash;
    }

    public string TxHash { get; }
}