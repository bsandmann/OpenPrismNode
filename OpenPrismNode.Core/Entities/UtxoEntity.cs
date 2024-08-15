namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class UtxoEntity
{
    /// <summary>
    /// PK
    /// </summary>
    public int UtxoEntityId { get; set; }

    /// <summary>
    /// Index of the operations related to other incoming or outgoing utxos for the same transaction
    /// </summary>
    [Column(TypeName = "smallint")]
    public int Index { get; set; }

    /// <summary>
    /// Flag if the Utxo is incoming or outgoing
    /// </summary>
    public bool IsOutgoing { get; set; }

    /// <summary>
    /// Value in lovelace moved in the utxo
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// Foreign key for TransactionEntity
    /// </summary>
    public byte[] TransactionHash { get; set; }

    public int BlockHeight { get; set; }
    public int BlockHashPrefix { get; set; }

    /// <summary>
    /// Navigation property to the associated TransactionEntity
    /// </summary>
    public TransactionEntity Transaction { get; set; }
    
    // Optional relationship with StakeAddressEntity
    public string? StakeAddress { get; set; }
    public StakeAddressEntity? StakeAddressEntity { get; set; }

    // Optional relationship with WalletAddressEntity
    public string? WalletAddress { get; set; }
    public WalletAddressEntity? WalletAddressEntity { get; set; }
}