namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618

/// <summary>
/// PrismUpdateDidEntities 
/// </summary>
public class PrismUpdateDidEntity : PrismBaseOperationEntity
{
    /// <summary>
    /// Previous OperationHash
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] PreviousOperationHash { get; set; }

    /// <summary>
    /// The signing Key used in the CreateDid Operation
    /// </summary>
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
    public PrismCreateDidEntity PrismCreateDidEntity { get; set; }
    
    /// <summary>
    /// Table of the services which have been updated in the operation
    /// </summary>
    public List<PrismServiceEntity> PrismServices { get; set; }
    
    /// <summary>
    /// Reference to the createDid operation
    /// </summary>
    public byte[] Did { get; set; }
}