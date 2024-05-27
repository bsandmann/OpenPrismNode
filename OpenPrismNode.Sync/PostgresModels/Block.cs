namespace OpenPrismNode.Sync.PostgresModels;

/// <summary>
/// block-Entity of the postgres table
/// </summary>
public class Block
{
    public long id { get; set; }
    public byte[] hash { get; set; } = new byte[32];
    public int epoch_no { get; set; }
    public long slot_no { get; set; }
    public int epoch_slot_no { get; set; }
    public int block_no { get; set; }
    public long previous_id { get; set; }
    public int size { get; set; }
    public DateTime time { get; set; }
    public long tx_count { get; set; }
}