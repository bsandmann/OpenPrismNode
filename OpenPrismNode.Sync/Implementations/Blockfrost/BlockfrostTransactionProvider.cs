namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Implementations.Blockfrost.Models;

/// <summary>
/// Implementation of the ITransactionProvider interface that retrieves data from the Blockfrost API.
/// </summary>
public class BlockfrostTransactionProvider : ITransactionProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlockfrostTransactionProvider> _logger;
    private readonly AppSettings _appSettings;

    // PRISM metadata key
    private const long PRISM_METADATA_KEY = 1587;
    
    public BlockfrostTransactionProvider(
        IHttpClientFactory httpClientFactory, 
        ILogger<BlockfrostTransactionProvider> logger,
        IOptions<AppSettings> appSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<Metadata>> GetMetadataFromTransaction(int txId, long key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Payment>>> GetPaymentDataFromTransaction(int txId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Transaction>>> GetTransactionsWithPrismMetadataForBlockId(int blockId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task<string> GetTransactionHashById(int txId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    // Helper to get a block hash by ID
    // In a real implementation, this would need to access a mapping table or service
    private async Task<string> GetBlockHashById(int blockId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


}