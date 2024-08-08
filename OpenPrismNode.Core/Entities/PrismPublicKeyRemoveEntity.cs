namespace OpenPrismNode.Core.Entities;

#pragma warning disable CS8618
/// <summary>
/// PrismPublicKeyRemoveEntity 
/// </summary>
public class PrismPublicKeyRemoveEntity
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
    public PrismUpdateDidEntity PrismUpdateDidEntity { get; set; }
}