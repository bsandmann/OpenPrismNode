namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionUtxos;

using System.Text.Json.Serialization;

public class ApiResponseUtxo
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("inputs")]
    public List<UtxoInput> Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public List<UtxoOutput> Outputs { get; set; }
}