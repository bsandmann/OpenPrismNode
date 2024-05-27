namespace OpenPrismNode.Core.Models;

using System.Text.Json;
using FluentResults;

public class PrismServiceEndpoints
{
    public Uri? Uri { get; set; }

    public Dictionary<string, object>? Json { get; set; }

    public List<Uri>? ListOfUris { get; set; }

    public static Result<PrismServiceEndpoints> Parse(string serviceEndpoint)
    {
        var isSingleUri = Uri.TryCreate(serviceEndpoint, UriKind.Absolute, out var uri);
        if (isSingleUri)
        {
            return new PrismServiceEndpoints { Uri = uri };
        }

        try
        {
            var deserializationResult = JsonSerializer.Deserialize<List<string>>(serviceEndpoint);
            if (deserializationResult is null)
            {
                return Result.Fail("Invalid ServiceEndpointUri: Deserialization failed.");
            }

            var uriConversionFailed = false;
            var uris = new List<Uri>();
            foreach (var deserializedItem in deserializationResult)
            {
                var isUri = Uri.TryCreate(deserializedItem, UriKind.Absolute, out var uriItem);
                if (!isUri)
                {
                    uriConversionFailed = true;
                }

                uris.Add(uriItem);
            }

            if (uriConversionFailed)
            {
                return Result.Fail("Invalid ServiceEndpointUri: Could not convert all items to Uri.");
            }

            return new PrismServiceEndpoints { ListOfUris = uris };
        }
        catch (Exception e)
        {
            return Result.Fail("Invalid ServiceEndpointUri: Could not parse as Uri or List of Uris.");
        }
    }
}