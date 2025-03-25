namespace OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// tx-Entity of the postgres table
/// </summary>
public class Transaction
{
    public int id { get; set; }
    public byte[] hash { get; set; } 
    public int block_index { get; set; }
    public decimal fee { get; set; }
    public int size { get; set; }

    public int? BlockNo { get; set; }

}