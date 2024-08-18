namespace OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Block-Entity of the postgres table from dbsync
/// </summary>
public class Block
{
    public int id { get; set; }
    public byte[] hash { get; set; } = new byte[32];
    public int epoch_no { get; set; }
    public int block_no { get; set; }
    public DateTime time { get; set; }
    public int tx_count { get; set; }
}