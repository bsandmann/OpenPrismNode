using System.Text.Json.Serialization;

public class UtxoOutput
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("amount")]
    public List<UtxoAmount> Amount { get; set; }

    [JsonPropertyName("output_index")]
    public int OutputIndex { get; set; }

    [JsonPropertyName("data_hash")]
    public string DataHash { get; set; }

    [JsonPropertyName("inline_datum")]
    public string InlineDatum { get; set; }

    [JsonPropertyName("collateral")]
    public bool Collateral { get; set; }

    [JsonPropertyName("reference_script_hash")]
    public string ReferenceScriptHash { get; set; }

    [JsonPropertyName("consumed_by_tx")]
    public string ConsumedByTx { get; set; }
}