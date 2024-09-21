namespace OpenPrismNode.Core.Commands.GetWallet;

public class GetWalletResponse
{
    public int WalletEntityId { get; set; }
    public string WalletId { get; set; }

    public long Balance { get; set; }

    public bool SyncingComplete { get; set; }

    public decimal? SyncProgress { get; set; }

    public string? FundingAddress { get; set; }

    public string Passphrase { get; set; }
}