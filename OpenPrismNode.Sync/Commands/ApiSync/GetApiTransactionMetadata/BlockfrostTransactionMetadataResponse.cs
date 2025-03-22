namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Response model for the Blockfrost transaction metadata API endpoint.
/// </summary>
public class BlockfrostTransactionMetadataResponse
{
    /// <summary>
    /// The transaction hash (hex encoded)
    /// </summary>
    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; } = string.Empty;

    /// <summary>
    /// The JSON metadata associated with the transaction
    /// This can be null if no metadata exists
    /// </summary>
    [JsonPropertyName("json_metadata")]
    public JsonElement? JsonMetadata { get; set; }
}
