namespace OpenPrismNode.Sync.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Provides access to blockchain transaction data through any supported data source.
/// This interface abstracts the retrieval of transactions and their metadata regardless of 
/// whether they come from a DbSync PostgreSQL database or a Blockfrost API.
/// </summary>
public interface ITransactionProvider
{
    /// <summary>
    /// Gets PRISM metadata from a transaction
    /// </summary>
    Task<Result<Metadata>> GetMetadataFromTransaction(int txId, byte[] txHash, long key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets payment data from a transaction
    /// </summary>
    Task<Result<Payment>> GetPaymentDataFromTransaction(int txId, byte[] txHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all transactions with PRISM metadata in a specific block
    /// </summary>
    Task<Result<List<Transaction>>> GetTransactionsWithPrismMetadataForBlockId(int blockId, int blockNo, CancellationToken cancellationToken = default);
}