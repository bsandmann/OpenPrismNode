namespace OpenPrismNode.Core.Models;

using System.Text.Json.Serialization;

public record PrismService
{
    [JsonConstructor]
    public PrismService(string serviceId, string type, PrismServiceEndpoints prismPrismServiceEndpoints)
    {
        this.ServiceId = serviceId;
        this.Type = type;
        this.PrismServiceEndpoints = prismPrismServiceEndpoints;
    }

    /// <summary>
    /// The did with #Linked-Domain
    /// </summary>
    public string ServiceId { get; }

    /// <summary>
    /// Usually 'LinkedDomains'
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// List of URIs
    /// </summary>
    public PrismServiceEndpoints PrismServiceEndpoints { get; set; }
}