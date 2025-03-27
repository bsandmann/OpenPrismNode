using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenPrismNode.Sync.Commands.ApiSync;



/// <summary>
/// Response model for the Blockfrost latest block API endpoint.
/// </summary>
public class BlockfrostBlockResponse
{
    /// <summary>
    /// Block creation time in UNIX time
    /// </summary>
    [JsonPropertyName("time")]
    public long Time { get; set; }

    /// <summary>
    /// Block number
    /// (Keep as int and do NOT convert null to 0 here—let it throw if null)
    /// </summary>
    [JsonPropertyName("height")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int Height { get; set; }

    /// <summary>
    /// Block hash (hex encoded)
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Slot number (allow null -> 0)
    /// </summary>
    [JsonPropertyName("slot")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int Slot { get; set; }

    /// <summary>
    /// Epoch number (allow null -> 0)
    /// </summary>
    [JsonPropertyName("epoch")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int Epoch { get; set; }

    /// <summary>
    /// Slot within the epoch (allow null -> 0)
    /// </summary>
    [JsonPropertyName("epoch_slot")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int EpochSlot { get; set; }

    /// <summary>
    /// Slot leader (pool ID)
    /// </summary>
    [JsonPropertyName("slot_leader")]
    public string SlotLeader { get; set; } = string.Empty;

    /// <summary>
    /// Block size in bytes (allow null -> 0)
    /// </summary>
    [JsonPropertyName("size")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int Size { get; set; }

    /// <summary>
    /// Transaction count (allow null -> 0)
    /// </summary>
    [JsonPropertyName("tx_count")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int TxCount { get; set; }

    /// <summary>
    /// Total output in Lovelace
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Total fees in Lovelace
    /// </summary>
    [JsonPropertyName("fees")]
    public string Fees { get; set; } = string.Empty;

    /// <summary>
    /// VRF key of the block
    /// </summary>
    [JsonPropertyName("block_vrf")]
    public string BlockVrf { get; set; } = string.Empty;

    /// <summary>
    /// Operational certificate
    /// </summary>
    [JsonPropertyName("op_cert")]
    public string OpCert { get; set; } = string.Empty;

    /// <summary>
    /// Operational certificate counter
    /// </summary>
    [JsonPropertyName("op_cert_counter")]
    public string OpCertCounter { get; set; } = string.Empty;

    /// <summary>
    /// Previous block hash (hex encoded)
    /// </summary>
    [JsonPropertyName("previous_block")]
    public string PreviousBlock { get; set; } = string.Empty;

    /// <summary>
    /// Next block hash (hex encoded)
    /// </summary>
    [JsonPropertyName("next_block")]
    public string? NextBlock { get; set; } = string.Empty;

    /// <summary>
    /// Number of confirmations (allow null -> 0)
    /// </summary>
    [JsonPropertyName("confirmations")]
    [JsonConverter(typeof(ZeroIfNullConverter))]
    public int Confirmations { get; set; }
}


/// <summary>
/// Converter that treats null JSON values as 0 when deserializing an int.
/// </summary>
public class ZeroIfNullConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            // Return 0 if the JSON value is explicitly null
            return 0;
        }

        // Otherwise parse as a normal int
        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // Normal write behavior
        writer.WriteNumberValue(value);
    }
}
