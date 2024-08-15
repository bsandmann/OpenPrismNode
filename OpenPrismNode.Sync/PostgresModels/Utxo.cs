namespace OpenPrismNode.Sync.PostgresModels;

using Core.Models;

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618

public class Utxo
{
    public int Index {get; set; }
    public long Value { get; set; }
    public WalletAddress WalletAddress { get; set; }
}