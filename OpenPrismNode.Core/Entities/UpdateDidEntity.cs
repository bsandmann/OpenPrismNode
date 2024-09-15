namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismUpdateDidEntities 
/// </summary>
public class UpdateDidEntity : BaseOperationEntity
{
    /// <summary>
    /// Previous OperationHash
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] PreviousOperationHash { get; set; }

    /// <summary>
    /// The signing Key used in the UpdateDid Operation
    /// </summary>
    [MaxLength(50)]
    public string SigningKeyId { get; set; }

    /// <summary>
    /// Table of the pairs of publicKeys / keyIds which should be added in the operation
    /// </summary>
    public List<PrismPublicKeyEntity> PrismPublicKeysToAdd { get; set; }

    /// <summary>
    /// Table to the keyIds which should be removed in the operation
    /// </summary>
    public List<PrismPublicKeyRemoveEntity> PrismPublicKeysToRemove { get; set; }

    /// <summary>
    /// Reference to the createDid operation which was saved on the blockchain prior
    /// </summary>
    public CreateDidEntity CreateDidEntity { get; set; }

    /// <summary>
    /// Table of the services which have been updated in the operation
    /// </summary>
    public List<PrismServiceEntity> PrismServices { get; set; }

    /// <summary>
    /// List of patched Contexts. List of strings. Cannot be null, but an empty list is allowed.
    /// </summary>
    public List<PatchedContextEntity> PatchedContexts { get; set; }

    // <summary>
    /// The Did created
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] Did { get; set; }
    
    /// <summary>
    /// FK
    /// </summary>
    // public OperationStatusEntity? OperationStatus { get; set; }
}