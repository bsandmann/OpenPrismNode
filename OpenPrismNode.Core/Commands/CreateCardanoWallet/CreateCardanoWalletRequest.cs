namespace OpenPrismNode.Core.Commands.CreateCardanoWallet;

using FluentResults;
using MediatR;

public class CreateCardanoWalletRequest : IRequest<Result<CreateCardanoWalletResponse>>
{
    /// <summary>
    /// Optional user-defined name for the wallet
    /// </summary> 
    public string? Name { get; set; }
}