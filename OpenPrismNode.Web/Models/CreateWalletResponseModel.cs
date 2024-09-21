namespace OpenPrismNode.Web.Models;

public class CreateWalletResponseModel
{
    /// <summary>
    /// A unique identifier for the wallet
    /// </summary>
    public string WalletId { get; set; }
    
    /// <summary>
    /// Need to restore the wallet on this system
    /// </summary>
    public List<string> Mnemonic { get; set; }
}