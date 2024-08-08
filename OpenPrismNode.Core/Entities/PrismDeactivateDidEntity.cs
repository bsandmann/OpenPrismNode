namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class PrismDeactivateDidEntity : PrismBaseOperationEntity
{
    /// <summary>
    /// Previous OperationHash
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] PreviousOperationHash { get; set; } = new byte[32];

    /// <summary>
    /// The signing Key used in the DeactivateDid Operation
    /// </summary>
    public string SigningKeyId { get; set; } = String.Empty;

    /// <summary>
    /// Reference to the createDid operation which was saved on the blockchain prior
    /// </summary>
    public PrismCreateDidEntity PrismCreateDidEntity { get; set; }

    /// <summary>
    /// Reference to the createDid operation
    /// </summary>
    public byte[] Did { get; set; } = new byte[32];
}