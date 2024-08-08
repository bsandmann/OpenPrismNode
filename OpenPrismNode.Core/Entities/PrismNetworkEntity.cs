namespace OpenPrismNode.Core.Entities;

using OpenPrismNode.Core.Models;

public class PrismNetworkEntity
{
    /// <summary>
    /// Name des Networks (0 = Testnet, 1 = Mainnet)
    /// </summary>
    public LedgerType NetworkType { get; set; }
    
    /// <summary>
    /// Last synced Block
    /// </summary>
    public DateTime LastSynced { get; set; }

    /// <summary>
    /// Referencing all epochs
    /// </summary>
    public List<PrismEpochEntity> Epochs { get; set; } = new List<PrismEpochEntity>();
}