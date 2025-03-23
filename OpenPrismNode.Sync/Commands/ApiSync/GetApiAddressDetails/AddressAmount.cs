namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiAddressDetails;

using System.Text.Json.Serialization;

public class AddressAmount
{
    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; }
}