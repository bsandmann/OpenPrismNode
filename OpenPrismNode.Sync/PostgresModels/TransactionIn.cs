namespace OpenPrismNode.Sync.PostgresModels;

public class TransactionIn
{
    public long id { get; set; }
    public long tx_in_id { get; set; }
    public long tx_out_id { get; set; }
    public int tx_out_index { get; set; }
}