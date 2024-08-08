namespace OpenPrismNode.Core.Entities;

/// <summary>
/// PrismPublicKeyRemoveEntity 
/// </summary>
public class PublicKeyRemoveEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public uint PrismPublicKeyRemoveEntityId { get; set; }
    
    /// <summary>
    /// The KeyId which should be removed e.g. issuing0
    /// </summary>
    public string KeyId { get; set; }
    
    /// <summary>
    /// For an update operation it is important to get the order of the different update-operation of the keys
    /// </summary>
    public int UpdateOperationOrder { get; set; }
    
    /// <summary>
    /// References to the linked operation which added this key
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public UpdateDidEntity UpdateDidEntity { get; set; }
}