namespace OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// tx_metadata-Entity of the postgres table
/// </summary>
public class Metadata
{
    public int id { get; set; }
    public decimal key { get; set; }
    public string json { get; set; } = String.Empty;
    public byte[] bytes { get; set; } = Array.Empty<byte>();
    public int tx_id { get; set; }
}