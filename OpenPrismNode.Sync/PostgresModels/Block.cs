namespace OpenPrismNode.Sync.PostgresModels;

/// <summary>
/// block-Entity of the postgres table
/// </summary>
public class Block
{
    public int id { get; set; }
    public byte[] hash { get; set; } = new byte[32];
    public int epoch_no { get; set; }
    public int slot_no { get; set; }
    public int epoch_slot_no { get; set; }
    public int block_no { get; set; }
    public int previous_id { get; set; }
    public int size { get; set; }
    public DateTime time { get; set; }
    public int tx_count { get; set; }
}