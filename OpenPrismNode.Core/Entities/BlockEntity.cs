#pragma warning disable CS8618
namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using Models;

/// <summary>
/// PrismBlockEntity 
/// </summary>
public class BlockEntity
{
    /// <summary>
    /// Height of the block (blocknumber)
    /// </summary>
    public required int BlockHeight { get; set; }

    public required int BlockHashPrefix { get; set; }

    /// <summary>
    /// The full blockhash
    /// </summary>
    [Column(TypeName = "bytea")]
    public required byte[] BlockHash { get; set; }

    /// <summary>
    /// Time when the block was created on the blockchain
    /// </summary>
    [Column(TypeName = "timestamp without time zone")]
    public required DateTime TimeUtc { get; set; }

    /// <summary>
    /// Number of transactions in this block
    /// </summary>
    [Column(TypeName = "smallint")]
    public required int TxCount { get; set; }

    /// <summary>
    /// When the block was created/updated in the database
    /// </summary>
    [Column(TypeName = "timestamp without time zone")]
    public DateTime? LastParsedOnUtc { get; set; }

    /// <summary>
    /// Transactions inside this block
    /// </summary>
    public ICollection<TransactionEntity> PrismTransactionEntities { get; set; } = new List<TransactionEntity>();

    /// <summary>
    /// Reference to connected Epoch
    /// </summary>
    public EpochEntity EpochEntity { get; set; }

    /// <summary>
    /// Epoch FK
    /// </summary>
    [Column(TypeName = "smallint")]
    public int EpochNumber { get; set; }
    
    public LedgerType Ledger { get; set; }
    
    /// <summary>
    /// Flag that this block is a part of a fork
    /// </summary>
    public bool IsFork { get; set; }

    // /// <summary>
    // /// Every block has a link to the next next block in the chain
    // /// This always should be just one block. It is implemented here as a list
    // /// because implementing as a list allows for forks to handle and detect those easier
    // /// </summary>
    public virtual ICollection<BlockEntity> NextBlocks { get; set; } = new List<BlockEntity>();

    /// <summary>
    /// Reference to the previous block
    /// Every Block should have a previous block except the first
    /// </summary>
    public BlockEntity? PreviousBlock { get; set; }

    /// <summary>
    /// Reference to the previous block
    /// Every Block should have a previous block except the first
    /// </summary>
    public int? PreviousBlockHeight { get; set; }

    public int? PreviousBlockHashPrefix { get; set; }


    [NotMapped]
    public string BlockHashHex
    {
        get => Convert.ToHexString(BlockHash);
        set => BlockHash = Convert.FromHexString(value);
    }

    public static int? CalculateBlockHashPrefix(byte[]? fullHash)
    {
        return fullHash != null ? BitConverter.ToInt32(fullHash, 0) : null;
    }

    public bool VerifyBlockHash(byte[] hash)
    {
        return CalculateBlockHashPrefix(hash) == BlockHashPrefix && hash.SequenceEqual(BlockHash);
    }
}