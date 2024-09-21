namespace OpenPrismNode.Core.Commands.RestoreWallet;

using CreateCardanoWallet;
using FluentResults;
using MediatR;

public class RestoreCardanoWalletRequest : IRequest<Result<RestoreCardanoWalletResponse>>
{
    /// <summary>
    /// Optional user-defined name for the wallet
    /// </summary> 
    public string? Name { get; set; }

    /// <summary>
    /// Recovery phrase
    /// </summary>
    public List<string> Mnemonic { get; set; }
}