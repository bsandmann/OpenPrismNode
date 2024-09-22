namespace OpenPrismNode.Core.Commands.RestoreWallet;

using CreateCardanoWallet;
using FluentResults;
using MediatR;

public class RestoreCardanoWalletRequest : IRequest<Result<RestoreCardanoWalletResponse>>
{
    public RestoreCardanoWalletRequest(List<string> mnemonic, string? name)
    {
        Mnemonic = mnemonic;
        Name = name;
    }

    /// <summary>
    /// Optional user-defined name for the wallet
    /// </summary> 
    public string? Name { get; }

    /// <summary>
    /// Recovery phrase
    /// </summary>
    public List<string> Mnemonic { get; }
}