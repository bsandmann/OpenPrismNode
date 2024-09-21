namespace OpenPrismNode.Core.Commands.GetWallet;

using FluentResults;
using MediatR;

public class GetWalletRequest : IRequest<Result<GetWalletResponse>>
{
   public string WalletId { get; set; } 
}