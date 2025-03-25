namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

public class TransactionBlockWrapper
{
    public TransactionBlockWrapper(string txHash, int? blockHeight)
    {
        TxHash = txHash;
        BlockHeight = blockHeight;
    }

    public string TxHash { get;  }
    public int? BlockHeight { get; set; }
}