namespace OpenPrismNode.Core.Commands.GetWalletTransactions;

using FluentResults;
using MediatR;

public class GetWalletTransactionsRequest : IRequest<Result<List<GetWalletTransactionsReponse>>>
{
    public GetWalletTransactionsRequest(string walletId)
    {
        WalletId = walletId;
    }

    public string WalletId { get; }
}