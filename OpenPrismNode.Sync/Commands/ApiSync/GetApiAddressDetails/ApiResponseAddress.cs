using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiAddressDetails
{
    public class ApiResponseAddress
    {
        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("amount")]
        public List<AddressAmount> Amount { get; set; }

        [JsonPropertyName("stake_address")]
        public string StakeAddress { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("script")]
        public bool Script { get; set; }
    }
}