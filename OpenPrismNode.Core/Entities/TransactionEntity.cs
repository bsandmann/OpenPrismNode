namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismTransactionEntity 
/// </summary>
public class TransactionEntity
{
    /// <summary>
    /// Identifier of the Transaction
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] TransactionHash { get; set; }

    /// <summary>
    /// Reference to the block this transactions lives in
    /// </summary>
    public BlockEntity BlockEntity { get; set; }

    /// <summary>
    /// Reference to the block this transactions lives in
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] BlockHash { get; set; }

    /// <summary>
    /// Fees creating this transaction in lovelace
    /// </summary>
    public long Fees { get; set; }

    /// <summary>
    /// Size of the transaction in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// BlockSequence number
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    /// The metadata label. might be identical for all PRISM operations
    /// </summary>
    public uint Label { get; set; }

    /// <summary>
    /// Optional CreateDid Operation
    /// </summary>
    public List<CreateDidEntity> PrismCreateDidEntities { get; set; }

    /// <summary>
    /// Optional UpdateDid Operation
    /// </summary>
    public List<UpdateDidEntity> PrismUpdateDidEntities { get; set; }
    
    /// <summary>
    /// Optional DeactivateDid Operation
    /// </summary>
    public List<DeactivateDidEntity> PrismDeactivateDidEntities { get; set; }

    /// <summary>
    /// Optional ProtocolVersionUpdate Operation
    /// </summary>
    public List<ProtocolVersionUpdateEntity> PrismProtocolVersionUpdateEntities { get; set; }

    /// <summary>
    /// List of the incoming Utxos of this transaction
    /// </summary>
    public List<IncomingUtxoEntity> UtxosIncoming { get; set; }
    
    /// <summary>
    /// List of the outgoing Utxos of this transaction
    /// </summary>
    public List<OutgoingUtxoEntity> UtxosOutgoing { get; set; }
}