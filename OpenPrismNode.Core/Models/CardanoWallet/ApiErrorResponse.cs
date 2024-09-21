using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class ApiErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
}