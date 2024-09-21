namespace OpenPrismNode.Core.Commands.GetWalletTransactions;

using FluentResults;
using MediatR;

public class GetWalletTransactionsRequest : IRequest<Result<List<GetWalletTransactionsReponse>>>
{
    public string WalletId { get; set; } 
}