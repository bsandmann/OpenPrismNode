namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a service entry in a DID Document.
    /// </summary>
    public class RegistrarService
    {
        /// <summary>
        /// Gets or sets the identifier for the service.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        /// <summary>
        /// Gets or sets the endpoint URL for the service.
        /// </summary>
        [JsonPropertyName("serviceEndpoint")]
        public string ServiceEndpoint { get; set; } = null!;

        /// <summary>
        /// Converts this service to a dictionary representation.
        /// </summary>
        /// <returns>A dictionary containing the service properties.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type,
                ["serviceEndpoint"] = ServiceEndpoint
            };
        }

        /// <summary>
        /// Creates a RegistrarService instance from a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary containing service properties.</param>
        /// <returns>A new RegistrarService instance, or null if the dictionary is invalid.</returns>
        public static RegistrarService? FromDictionary(Dictionary<string, object> dictionary)
        {
            if (!dictionary.TryGetValue("id", out var idObj) || string.IsNullOrEmpty(idObj?.ToString()))
                return null;

            if (!dictionary.TryGetValue("type", out var typeObj) || string.IsNullOrEmpty(typeObj?.ToString()))
                return null;

            if (!dictionary.TryGetValue("serviceEndpoint", out var endpointObj) || string.IsNullOrEmpty(endpointObj?.ToString()))
                return null;

            return new RegistrarService
            {
                Id = idObj.ToString()!,
                Type = typeObj.ToString()!,
                ServiceEndpoint = endpointObj.ToString()!
            };
        }
    }
}