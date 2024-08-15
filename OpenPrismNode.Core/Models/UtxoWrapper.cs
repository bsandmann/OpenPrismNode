namespace OpenPrismNode.Core.Models;

using Entities;

public class UtxoWrapper
{
    public int Index { get; set; }
    public int Value { get; set; }
    
    public bool IsOutgoing { get; set; }
    public WalletAddress WalletAddress { get; set; }
}