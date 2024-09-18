namespace OpenPrismNode.Core.Commands.GetWalletState;

using FluentResults;
using MediatR;

public class GetWalletStateRequest : IRequest<Result<GetWalletStateResponse>>
{
   public string WalletKey { get; set; } 
}