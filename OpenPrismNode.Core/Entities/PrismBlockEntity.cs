#pragma warning disable CS8618
namespace OpenPrismNode.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// PrismBlockEntity 
/// </summary>
public class PrismBlockEntity
{
    /// <summary>
    /// Hash of the block as hex
    /// </summary>
    [Column(TypeName = "binary(32)")]
    public byte[] BlockHash { get; set; }

    /// <summary>
    /// Height of the block (blocknumber)
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Slot in the epoch
    /// </summary>
    public long EpochSlot { get; set; }

    /// <summary>
    /// Time when the block was created on the blockchain
    /// </summary>
    public DateTime TimeUtc { get; set; }

    /// <summary>
    /// Number of transactions in this block
    /// </summary> 
    public uint TxCount { get; set; }

    /// <summary>
    /// When the block was created/updated in the database
    /// </summary>
    public DateTime LastParsedOnUtc { get; set; }


    /// <summary>
    /// Transactions inside this block
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<PrismTransactionEntity>? PrismTransactionEntities { get; set; }

    /// <summary>
    /// Reference to connected Epoch
    /// </summary>
    public PrismEpochEntity PrismEpochEntity { get; set; }

    /// <summary>
    /// Epoch FK
    /// </summary>
    public uint Epoch { get; set; }

    /// <summary>
    /// Every block has a link to the next next block in the chain
    /// This always should be just one block. It is implemented here as a list
    /// because implementing as a list allows for forks to handle and detect those easier
    /// </summary>
    public List<PrismBlockEntity> NextBlocks { get; set; } = new List<PrismBlockEntity>();

    /// <summary>
    /// Reference to the previous block
    /// Every Block should have a previous block except the first
    /// </summary>
    public PrismBlockEntity? PreviousBlock { get; set; }

    /// <summary>
    /// Reference to the previous block
    /// Every Block should have a previous block except the first
    /// </summary>
    public byte[]? PreviousBlockHash { get; set; }
}