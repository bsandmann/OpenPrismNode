namespace OpenPrismNode.Web.Models;

public class GetWalletResponseModel
{
    public string WalletId { get; set; }

    public long Balance { get; set; }

    public bool SyncingComplete { get; set; }

    public decimal? SyncProgress { get; set; }

    public string? FundingAddress { get; set; }
}