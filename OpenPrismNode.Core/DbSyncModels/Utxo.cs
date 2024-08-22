namespace OpenPrismNode.Core.DbSyncModels;

using OpenPrismNode.Core.Models;

public class Utxo
{
    public int Index {get; set; }
    public long Value { get; set; }
    public WalletAddress WalletAddress { get; set; }
}