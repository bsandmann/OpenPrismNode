namespace OpenPrismNode.Core.Entities;

public class PrismServiceEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public uint PrismServiceEntityKey { get; set; }
    /// <summary>
    /// Name of the service 
    /// </summary>
    public string ServiceId { get; set; }
    
    /// <summary>
    /// Service type eg. LinkedDomains
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// List of endpoints delimited by '||'
    /// </summary>
    public string? ServiceEndpoints { get; set; }

    /// <summary>
    /// When a remove service-action is performed this flag gets set
    /// </summary>
    public bool Removed { get; set; }
    
    /// <summary>
    /// When an update occures this flag is set
    /// </summary>
    public bool Updated { get; set; }
    
    /// <summary>
    /// For an update operation it is important to get the order of the different update-operation of the keys
    /// </summary>
    public int? UpdateOperationOrder { get; set; }

    /// <summary>
    /// References to the linked operation which created this key
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public CreateDidEntity? PrismCreateDidEntity { get; set; }

    // /// <summary>
    // /// References to the linked operation which added this key
    // /// </summary>
    // // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public UpdateDidEntity? PrismUpdateDidEntity { get; set; }
}