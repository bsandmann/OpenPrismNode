namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismPublicKeyRemoveEntity 
/// </summary>
public class PrismPublicKeyRemoveEntity
{
    /// <summary>
    /// Identifier
    /// </summary>
    public int PrismPublicKeyRemoveEntityId { get; set; }
    
    /// <summary>
    /// The KeyId which should be removed e.g. issuing0
    /// </summary>
    [MaxLength(50)]
    public string KeyId { get; set; }
    
    /// <summary>
    /// For an update operation it is important to get the order of the different update-operation of the keys
    /// </summary>
    [Column(TypeName = "smallint")]
    public int UpdateOperationOrder { get; set; }
    
    /// <summary>
    /// References to the linked operation which added this key
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public UpdateDidEntity UpdateDidEntity { get; set; }
    
    /// <summary>
    /// References to the linked operation which added this key
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] UpdateDidOperationHash { get; set; } 
}