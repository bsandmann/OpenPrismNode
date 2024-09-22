namespace OpenPrismNode.Core.Commands.GetWallet;

public class GetWalletResponse
{
    public int WalletEntityId { get; init; }
    public string WalletId { get; init; }
    public long Balance { get; init; }
    public bool SyncingComplete { get; init; }
    public decimal? SyncProgress { get; init; }
    public string? FundingAddress { get; init; }
    public string Passphrase { get; init; }
}