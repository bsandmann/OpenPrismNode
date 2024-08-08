namespace OpenPrismNode.Core.Entities;
#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismCreateDidEntities 
/// </summary>
public class CreateDidEntity : BaseOperationEntity
{
    /// <summary>
    /// The Did created
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] Did { get; set; }

    /// <summary>
    /// The signing Key used in the CreateDid Operation
    /// </summary>
    public string SigningKeyId { get; set; }

    /// <summary>
    /// Table of the pairs of publicKeys / keyIds which have been created in the operation
    /// </summary>
    public List<PrismPublicKeyEntity> PrismPublicKeys { get; set; }
   
    /// <summary>
    /// Table of the services which have been created in the operation
    /// </summary>
    public List<PrismServiceEntity> PrismServices { get; set; }
    
    /// <summary>
    /// Reference to all updates
    /// </summary>
    public List<UpdateDidEntity> DidUpdates { get; set; }

    /// <summary>
    /// Reference to possible Deactivation
    /// </summary>
    public DeactivateDidEntity? DidDeactivation { get; set; }
}