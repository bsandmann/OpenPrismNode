namespace OpenPrismNode.Web.Models;

public class CreateWalletResponseModel
{
    public List<string> Mnemonic { get; set; }

    public string WalletKey { get; set; }
}