﻿namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public abstract class PrismBaseOperationEntity
{
    /// <summary>
    /// Identifier
    /// Hash of the Prism-operation
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] OperationHash { get; set; } = null!;

    /// <summary>
    /// In case multiple PRISM-Operations are in one transaction, this number definde the position in the transaction
    /// eg. a DidCreateOperation may have 0 and the following IssueCredentialBatchOperation has 1.
    /// </summary>
    public int OperationSequenceNumber { get; set; }

    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    public PrismTransactionEntity PrismTransactionEntity { get; set; } = null!;

    /// <summary>
    /// Reference of the connected Transaction
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public byte[] TransactionHash { get; set; } = null!;
}