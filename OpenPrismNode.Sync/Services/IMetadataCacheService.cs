using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace OpenPrismNode.Sync.Services
{
    /// <summary>
    /// Interface for API transaction cache operations
    /// </summary>
    public interface IMetadataCacheService
    {
        /// <summary>
        /// Rebuilds the cache for API transactions
        /// </summary>
        Task<Result> RebuildCacheAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the cache for API transactions
        /// </summary>
        Task<Result> UpdateCacheAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Rolls back the cache for API transactions
        /// </summary>
        Task<Result> RollbackCacheAsync(int blocksRolledBack, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds the block number of the last metadata cache update
        /// </summary>
        void UpdateBlockNoOfMetadataCacheUpdate(int blockNo);
    }
}
