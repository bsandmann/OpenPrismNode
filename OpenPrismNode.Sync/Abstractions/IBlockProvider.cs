namespace OpenPrismNode.Sync.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Core.Models;
using FluentResults;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Provides access to blockchain block data through any supported data source.
/// This interface abstracts the retrieval of blocks regardless of whether they come from 
/// a DbSync PostgreSQL database or a Blockfrost API.
/// </summary>
public interface IBlockProvider
{
    /// <summary>
    /// Gets the most recent block (tip) of the blockchain
    /// </summary>
    Task<Result<Block>> GetBlockTip(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a block by its block number (height)
    /// </summary>
    Task<Result<Block>> GetBlockByNumber(int blockNo, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a block by its internal ID
    /// </summary>
    Task<Result<Block>> GetBlockById(int blockId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets multiple blocks by their block numbers
    /// </summary>
    Task<Result<List<Block>>> GetBlocksByNumbers(int firstBlockNo, int count, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the first block of an epoch
    /// </summary>
    Task<Result<Block>> GetFirstBlockOfEpoch(int epochNo, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the next block containing PRISM metadata after a specified block height
    /// </summary>
    Task<Result<Block>> GetNextBlockWithPrismMetadata(int afterBlockNo, int maxBlockNo, LedgerType ledgerType, int metadataKey, CancellationToken cancellationToken = default);
}