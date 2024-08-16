namespace OpenPrismNode.Core.Models;

public class UtxoWrapper
{
    public int Index { get; set; }
    public int Value { get; set; }
    
    public bool IsOutgoing { get; set; }
    public WalletAddress WalletAddress { get; set; }
}