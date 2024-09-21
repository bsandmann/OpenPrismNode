namespace OpenPrismNode.Web.Models;

public class RestoreWalletRequestModel
{
    /// <summary>
    /// Optional user-defined name for the wallet
    /// </summary> 
    public string? Name { get; set; }

    /// <summary>
    /// Recovery phrase. Need to restore the wallet on a new system
    /// </summary> 
    public List<string> Mnemonic { get; set; }
}