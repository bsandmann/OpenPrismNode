namespace OpenPrismNode.Core.Commands.CreateCardanoWallet;

public class CreateCardanoWalletResponse
{
    public string WalletId { get; init; }
    public List<string> Mnemonic { get; init; }
    public int WalletEntityId { get; init; }
}