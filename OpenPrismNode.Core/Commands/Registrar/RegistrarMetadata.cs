namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents metadata related to DID registration or the DID document itself.
    /// Using a dictionary for flexibility.
    /// </summary>
    [JsonConverter(typeof(RegistrarMetadataConverter))]
    public class RegistrarMetadata : Dictionary<string, object?>
    {
        public RegistrarMetadata() : base(StringComparer.Ordinal) { }
        public RegistrarMetadata(IDictionary<string, object?> dictionary) : base(dictionary, StringComparer.Ordinal) { }
    }

    // Basic converter, can be expanded if complex handling is needed
    public class RegistrarMetadataConverter : JsonConverter<RegistrarMetadata>
    {
        public override RegistrarMetadata? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for RegistrarMetadata");
            }
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options);
            return dictionary == null ? null : new RegistrarMetadata(dictionary);
        }

        public override void Write(Utf8JsonWriter writer, RegistrarMetadata value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (IDictionary<string, object?>)value, options);
        }
    }
}