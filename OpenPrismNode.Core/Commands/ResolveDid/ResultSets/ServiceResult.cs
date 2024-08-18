namespace OpenPrismNode.Core.Commands.ResolveDid.ResultSets;

public class ServiceResult
{
    /// <summary>
    /// Name of the service 
    /// </summary>
    public string ServiceId { get; set; }

    /// <summary>
    /// Service type eg. LinkedDomains
    /// </summary>
    public string? Type { get; set; }

    public Uri? Uri { get; set; }

    public List<Uri>? ListOfUris { get; set; }
    public string? JsonData { get; set; }
    
    /// <summary>
    /// When a remove service-action is performed this flag gets set
    /// </summary>
    public bool Removed { get; set; }

    /// <summary>
    /// When an update occures this flag is set
    /// </summary>
    public bool Updated { get; set; }

    /// <summary>
    /// Optional property for the updateOperation
    /// </summary>
    public int? UpdateOperationOrder { get; set; }
}