namespace OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// epoch-Entity of the postgres table
/// </summary>
public class Epoch
{
    public int id { get; set; }
    public decimal out_sum { get; set; }
    public decimal fees { get; set; }
    public int tx_count { get; set; }
    public int blk_count { get; set; }
    public int no { get; set; }
    public DateTime start_time { get; set; }
    public DateTime end_time { get; set; }
}