namespace OpenPrismNode.Core.DbSyncModels;

public class Payment
{
    public List<Utxo> Incoming { get; set; } = new List<Utxo>();
    public List<Utxo> Outgoing { get; set; } = new List<Utxo>();
}