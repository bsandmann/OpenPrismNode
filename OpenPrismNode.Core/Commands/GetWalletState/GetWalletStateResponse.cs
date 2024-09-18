namespace OpenPrismNode.Core.Commands.GetWalletState;

public class GetWalletStateResponse
{
    public string WalletKey { get; set; }

    public long Balance { get; set; }

    public bool SyncingComplete { get; set; }
}