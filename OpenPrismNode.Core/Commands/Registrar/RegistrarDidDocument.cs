namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Placeholder for DID Document structure.
    /// A real implementation would likely use a library that models DID Core specs.
    /// Using Dictionary for flexibility for now.
    /// </summary>
    [JsonConverter(typeof(RegistrarDidDocumentConverter))] // Allows receiving/sending raw JSON object
    public class RegistrarDidDocument : Dictionary<string, object>
    {
        // You might add specific strongly-typed properties later if needed,
        // but Dictionary<string, object> handles arbitrary DID doc content.
        [JsonIgnore] // Example: helper property not part of JSON
        public string? Id => ContainsKey("id") ? this["id"]?.ToString() : null;
    }

    /// <summary>
    /// Handles serialization/deserialization of the essentially raw JSON object
    /// for RegistrarDidDocument.
    /// </summary>
    public class RegistrarDidDocumentConverter : JsonConverter<RegistrarDidDocument>
    {
        public override RegistrarDidDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
            if (dictionary == null) return null;

            var didDoc = new RegistrarDidDocument();
            foreach (var kvp in dictionary)
            {
                didDoc.Add(kvp.Key, kvp.Value);
            }
            return didDoc;
        }

        public override void Write(Utf8JsonWriter writer, RegistrarDidDocument value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (Dictionary<string, object>)value, options);
        }
    }
}