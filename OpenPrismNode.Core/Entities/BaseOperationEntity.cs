namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public abstract class BaseOperationEntity
{
    /// <summary>
    /// Identifier
    /// Hash of the Prism-operation
    /// </summary>
    [Column(TypeName = "bytea")]
    public byte[] OperationHash { get; set; } = null!;

    /// <summary>
    /// In case multiple PRISM-Operations are in one transaction, this number definde the position in the transaction
    /// eg. a DidCreateOperation may have 0 and the following IssueCredentialBatchOperation has 1.
    /// </summary>
    public int OperationSequenceNumber { get; set; }

    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    public TransactionEntity TransactionEntity { get; set; } = null!;

    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Column(TypeName = "bytea")]
    public byte[] TransactionHash { get; set; } = null!;
    
    public int BlockHeight { get; set; }
    
    public int BlockHashPrefix { get; set; }
}