namespace OpenPrismNode.Core.Models;

using System.Text.Json.Serialization;

public record PrismService
{
    [JsonConstructor]
    public PrismService(string serviceId, string type, ServiceEndpoints serviceEndpoints)
    {
        this.ServiceId = serviceId;
        this.Type = type;
        this.ServiceEndpoints = serviceEndpoints;
    }

    /// <summary>
    /// The serviceID fragment
    /// </summary>
    public string ServiceId { get; }

    /// <summary>
    /// Usually 'LinkedDomains'
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// List of URIs
    /// </summary>
    public ServiceEndpoints ServiceEndpoints { get; set; }
}