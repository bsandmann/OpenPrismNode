namespace OpenPrismNode.Core.Entities;
#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismCreateDidEntities 
/// </summary>
public class CreateDidEntity : BaseOperationEntity
{
    /// <summary>
    /// The signing Key used in the CreateDid Operation
    /// </summary>
    [MaxLength(50)]
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
    /// Optional List of patched Contexts. List of strings
    /// </summary>
    public PatchedContextEntity? PatchedContext { get; set; }
    
    /// <summary>
    /// Reference to possible Deactivation
    /// </summary>
    public DeactivateDidEntity? DidDeactivation { get; set; }
}