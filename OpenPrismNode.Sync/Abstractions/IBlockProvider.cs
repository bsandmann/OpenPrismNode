namespace OpenPrismNode.Sync.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Commands.DbSync.GetNextBlockWithPrismMetadata;
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
    Task<Result<Block>> GetBlockTip(CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets a block by its block number (height)
    /// </summary>
    Task<Result<Block>> GetBlockByNumber(int blockNo, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets a block by its internal ID
    /// </summary>
    Task<Result<Block>> GetBlockById(int blockId, CancellationToken cancellationToken, int? blockNo = null);
    
    /// <summary>
    /// Gets multiple blocks by their block numbers
    /// </summary>
    Task<Result<List<Block>>> GetBlocksByNumbers(int firstBlockNo, int count, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the first block of an epoch
    /// </summary>
    Task<Result<Block>> GetFirstBlockOfEpoch(int epochNo, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the next block containing PRISM metadata after a specified block height
    /// </summary>
    Task<Result<GetNextBlockWithPrismMetadataResponse>> GetNextBlockWithPrismMetadata(int afterBlockNo, int maxBlockNo, LedgerType ledgerType, int metadataKey, int currentBlockTip, CancellationToken cancellationToken);
}