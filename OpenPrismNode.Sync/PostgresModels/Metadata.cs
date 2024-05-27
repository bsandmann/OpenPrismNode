namespace OpenPrismNode.Sync.PostgresModels;

/// <summary>
/// tx_metadata-Entity of the postgres table
/// </summary>
public class Metadata
{
    public long id { get; set; }
    public decimal key { get; set; }
    public string json { get; set; } = String.Empty;
    public byte[] bytes { get; set; } = Array.Empty<byte>();
    public long tx_id { get; set; }
}