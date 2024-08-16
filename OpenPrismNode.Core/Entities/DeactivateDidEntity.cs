namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DeactivateDidEntity : BaseOperationEntity
{
    /// <summary>
    /// Previous OperationHash
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] PreviousOperationHash { get; set; }

    /// <summary>
    /// The signing Key used in the DeactivateDid Operation
    /// </summary>
    [MaxLength(50)]
    public string SigningKeyId { get; set; }

    /// <summary>
    /// Reference to the createDid operation which was saved on the blockchain prior
    /// </summary>
    public CreateDidEntity CreateDidEntity { get; set; }

    // <summary>
    /// The Did created
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] Did { get; set; }
}