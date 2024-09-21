namespace OpenPrismNode.Core.Commands.GetWallets;

using FluentResults;
using GetWallet;
using MediatR;

public class GetWalletsRequest : IRequest<Result<List<GetWalletResponse>>>
{
}