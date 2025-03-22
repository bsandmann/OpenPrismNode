namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Implementations.Blockfrost.Models;

/// <summary>
/// Implementation of the ITransactionProvider interface that retrieves data from the Blockfrost API.
/// </summary>
public class BlockfrostTransactionProvider : ITransactionProvider
{
    private readonly BlockfrostApiClient _apiClient;
    private readonly ILogger<BlockfrostTransactionProvider> _logger;

    // PRISM metadata key
    private const long PRISM_METADATA_KEY = 1587;
    
    public BlockfrostTransactionProvider(BlockfrostApiClient apiClient, ILogger<BlockfrostTransactionProvider> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<Metadata>> GetMetadataFromTransaction(int txId, long key, CancellationToken cancellationToken = default)
    {
        // In Blockfrost, we need the transaction hash instead of an internal ID
        // This is a limitation of the current architecture - in a real implementation
        // you'd need a way to map DB Sync transaction IDs to hash or store/cache the mapping
        string txHash = await GetTransactionHashById(txId, cancellationToken);
        if (string.IsNullOrEmpty(txHash))
        {
            return Result.Fail<Metadata>($"Could not find transaction hash for ID {txId}");
        }

        var result = await _apiClient.GetListAsync<BlockfrostMetadata>($"txs/{txHash}/metadata", cancellationToken);
        if (result.IsFailed)
        {
            _logger.LogError("Failed to get metadata for transaction {TxId} from Blockfrost API: {Error}", 
                txId, result.Errors.FirstOrDefault()?.Message);
            return Result.Fail<Metadata>(result.Errors);
        }

        // Find metadata with the matching key
        var metadata = result.Value.FirstOrDefault(m => m.Label == key.ToString());
        if (metadata == null)
        {
            return Result.Fail<Metadata>($"No metadata with key {key} found for transaction {txId}");
        }

        return Result.Ok(new Metadata
        {
            tx_id = txId,
            key = key,
            json = metadata.JsonMetadata
        });
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Payment>>> GetPaymentDataFromTransaction(int txId, CancellationToken cancellationToken = default)
    {
        // Similar limitation as above - need transaction hash
        string txHash = await GetTransactionHashById(txId, cancellationToken);
        if (string.IsNullOrEmpty(txHash))
        {
            return Result.Fail<IEnumerable<Payment>>($"Could not find transaction hash for ID {txId}");
        }

        // Get UTXOs
        var utxosResult = await _apiClient.GetListAsync<dynamic>($"txs/{txHash}/utxos", cancellationToken);
        if (utxosResult.IsFailed)
        {
            _logger.LogError("Failed to get UTXOs for transaction {TxId} from Blockfrost API: {Error}", 
                txId, utxosResult.Errors.FirstOrDefault()?.Message);
            return Result.Fail<IEnumerable<Payment>>(utxosResult.Errors);
        }

        // Convert to Payments (this is simplified - you'd need to map the actual UTXO structure)
        var payments = new List<Payment>();
        // Implementation would map UTXOs to Payments based on the API response structure

        return Result.Ok<IEnumerable<Payment>>(payments);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Transaction>>> GetTransactionsWithPrismMetadataForBlockId(int blockId, CancellationToken cancellationToken = default)
    {
        // Get the block hash for the given block ID 
        // In real implementation, you'd need a mapping strategy for DB Sync block IDs to Blockfrost block hashes
        string blockHash = await GetBlockHashById(blockId, cancellationToken);
        if (string.IsNullOrEmpty(blockHash))
        {
            return Result.Fail<IEnumerable<Transaction>>($"Could not find block hash for ID {blockId}");
        }

        // Get all transactions in the block
        var txsResult = await _apiClient.GetListAsync<BlockfrostTransaction>($"blocks/{blockHash}/txs", cancellationToken);
        if (txsResult.IsFailed)
        {
            _logger.LogError("Failed to get transactions for block {BlockId} from Blockfrost API: {Error}", 
                blockId, txsResult.Errors.FirstOrDefault()?.Message);
            return Result.Fail<IEnumerable<Transaction>>(txsResult.Errors);
        }

        var transactions = new List<Transaction>();
        
        // For each transaction, check if it has PRISM metadata
        foreach (var tx in txsResult.Value)
        {
            var metadataResult = await _apiClient.GetListAsync<BlockfrostMetadata>($"txs/{tx.Hash}/metadata", cancellationToken);
            if (metadataResult.IsSuccess)
            {
                // Check if any metadata has the PRISM key
                bool hasPrismMetadata = metadataResult.Value.Any(m => m.Label == PRISM_METADATA_KEY.ToString());
                if (hasPrismMetadata)
                {
                    // Add transaction to the result list
                    transactions.Add(new Transaction
                    {
                        id = -1, // Placeholder for ID
                        hash = ConvertHexStringToByteArray(tx.Hash),
                        // block_id is commented out in the model
                        block_index = tx.Index,
                        fee = 0, // This would need real calculation in proper implementation
                        size = 0 // This would need real calculation in proper implementation
                    });
                }
            }
        }

        return Result.Ok<IEnumerable<Transaction>>(transactions);
    }

    // Helper to get a transaction hash by ID
    // In a real implementation, this would need to access a mapping table or service
    private async Task<string> GetTransactionHashById(int txId, CancellationToken cancellationToken)
    {
        // Placeholder implementation
        // In reality, you'd need to maintain a mapping between DB Sync IDs and Blockfrost hashes
        // or retrieve the hash from your own database
        _logger.LogWarning("GetTransactionHashById is using a placeholder implementation");
        return null;
    }

    // Helper to get a block hash by ID
    // In a real implementation, this would need to access a mapping table or service
    private async Task<string> GetBlockHashById(int blockId, CancellationToken cancellationToken)
    {
        // Placeholder implementation
        // In reality, you'd need to maintain a mapping between DB Sync IDs and Blockfrost hashes
        // or retrieve the hash from your own database
        _logger.LogWarning("GetBlockHashById is using a placeholder implementation");
        return null;
    }

    // Helper to convert hex string to byte array for hash values
    private byte[] ConvertHexStringToByteArray(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return new byte[0];

        // Remove "0x" prefix if present
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}