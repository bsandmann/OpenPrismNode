namespace OpenPrismNode.Sync.Commands.ApiSync;

using System.Text.Json.Serialization;

/// <summary>
/// Represents each item in the <see cref="BlockfrostTransactionResponse.OutputAmount"/> array.
/// </summary>
public class BlockfrostTransactionOutputAmount
{
    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; }
}