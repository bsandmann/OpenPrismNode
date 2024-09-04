using System.Text.Json;
using System.Text.Json.Serialization;
using OpenPrismNode.Core.Models.DidDocument;

public class ServiceEndpointConverter : JsonConverter<DidDocumentService>
{
    public override DidDocumentService Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Implement deserialization logic if needed
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, DidDocumentService value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("id", value.Id);
        
        if (!string.IsNullOrEmpty(value.Type))
        {
            writer.WriteString("type", value.Type);
        }

        writer.WritePropertyName("serviceEndpoint");
        if (value.ServiceEndpointStringList != null)
        {
            JsonSerializer.Serialize(writer, value.ServiceEndpointStringList, options);
        }
        else if (value.ServiceEndpointString != null)
        {
            writer.WriteStringValue(value.ServiceEndpointString);
        }
        else if (value.ServiceEndpointObject != null)
        {
            JsonSerializer.Serialize(writer, value.ServiceEndpointObject, options);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteEndObject();
    }
}