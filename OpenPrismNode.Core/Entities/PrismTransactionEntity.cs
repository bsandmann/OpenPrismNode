namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
/// <summary>
/// PrismTransactionEntity 
/// </summary>
public class PrismTransactionEntity
{
    /// <summary>
    /// Identifier of the Transaction
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] TransactionHash { get; set; }

    /// <summary>
    /// Reference to the block this transactions lives in
    /// </summary>
    public PrismBlockEntity PrismBlockEntity { get; set; }

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
    public List<PrismCreateDidEntity> PrismCreateDidEntities { get; set; }

    /// <summary>
    /// Optional UpdateDid Operation
    /// </summary>
    public List<PrismUpdateDidEntity> PrismUpdateDidEntities { get; set; }
    
    /// <summary>
    /// Optional DeactivateDid Operation
    /// </summary>
    public List<PrismDeactivateDidEntity> PrismDeactivateDidEntities { get; set; }

    /// <summary>
    /// Optional ProtocolVersionUpdate Operation
    /// </summary>
    public List<PrismProtocolVersionUpdateEntity> PrismProtocolVersionUpdateEntities { get; set; }

    /// <summary>
    /// List of the incoming Utxos of this transaction
    /// </summary>
    public List<PrismIncomingUtxoEntity> UtxosIncoming { get; set; }
    
    /// <summary>
    /// List of the outgoing Utxos of this transaction
    /// </summary>
    public List<PrismOutgoingUtxoEntity> UtxosOutgoing { get; set; }
}