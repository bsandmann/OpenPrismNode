namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismTransactionEntity 
/// </summary>
public class TransactionEntity
{
    /// <summary>
    /// Identifier of the Transaction
    /// </summary>
    [Column(TypeName = "bytea")]
    public required byte[] TransactionHash { get; set; }

    /// <summary>
    /// Reference to the block this transactions lives in
    /// </summary>
    public BlockEntity BlockEntity { get; set; }

    /// <summary>
    /// Foreign key for BlockEntity (composite key)
    /// </summary>
    public int BlockHeight { get; set; }

    public int BlockHashPrefix { get; set; }

    /// <summary>
    /// Fees creating this transaction in lovelace
    /// </summary>
    public int Fees { get; set; }

    /// <summary>
    /// Size of the transaction in bytes
    /// </summary>
    [Column(TypeName = "smallint")]
    public int Size { get; set; }

    /// <summary>
    /// BlockSequence number
    /// </summary>
    [Column(TypeName = "smallint")]
    public int Index { get; set; }

    /// <summary>
    /// Optional CreateDid Operation
    /// </summary>
    public ICollection<CreateDidEntity> CreateDidEntities { get; set; }
    //
    // /// <summary>
    // /// Optional UpdateDid Operation
    // /// </summary>
    // public List<UpdateDidEntity> UpdateDidEntities { get; set; }
    //
    // /// <summary>
    // /// Optional DeactivateDid Operation
    // /// </summary>
    // public List<DeactivateDidEntity> DeactivateDidEntities { get; set; }
    //
    // /// <summary>
    // /// Optional ProtocolVersionUpdate Operation
    // /// </summary>
    // public List<ProtocolVersionUpdateEntity> ProtocolVersionUpdateEntities { get; set; }

    /// <summary>
    /// List of the incoming or outgoing Utxos of this transaction
    /// </summary>
    public ICollection<UtxoEntity> Utxos { get; set; } = new List<UtxoEntity>();

    [NotMapped]
    public string TransactionHashHex
    {
        get => Convert.ToHexString(TransactionHash);
        set => TransactionHash = Convert.FromHexString(value);
    }
}