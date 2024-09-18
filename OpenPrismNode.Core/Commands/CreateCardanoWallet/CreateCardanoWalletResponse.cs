namespace OpenPrismNode.Core.Commands.CreateCardanoWallet;

public class CreateCardanoWalletResponse
{
    public string WalletId { get; set; }

    public List<string> Mnemonic { get; set; }

    public string WalletKey { get; set; }
}