namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using Models;

public class OperationStatusEntity
{
    /// <summary>
    /// PK - hash of the fully signed operation
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] OperationStatusId { get; set; }

    /// <summary>
    /// Identifier
    /// Hash of the Prism-operation which has the specific status
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] OperationHash { get; set; } = null!;

    /// <summary>
    /// Created
    /// </summary> 
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Last updated
    /// </summary> 
    public DateTime? LastUpdatedUtc { get; set; }

    /// <summary>
    /// Status of the operation
    /// </summary>
    public OperationStatusEnum Status { get; set; }

    /// <summary>
    /// Type of operation which is described by this Status / OperationHash
    /// </summary>
    public OperationTypeEnum OperationType { get; set; }

    // FKs 
    // public CreateDidEntity? CreateDidEntity { get; set; }
    // public UpdateDidEntity? UpdateDidEntity { get; set; }
    // public DeactivateDidEntity? DeactivateDidEntity { get; set; }
}