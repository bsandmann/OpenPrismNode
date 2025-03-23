namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Grpc.Models;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Retrieves transaction metadata from the Blockfrost API using a transaction hash.
/// </summary>
public class GetApiMetadataHandler : IRequestHandler<GetApiMetadataRequest, Result<Metadata?>>
{
    private readonly ILogger<GetApiMetadataHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IAppCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiMetadataHandler"/> class.
    /// </summary>
    public GetApiMetadataHandler(
        ILogger<GetApiMetadataHandler> logger,
        IOptions<AppSettings> appSettings,
        IAppCache cache)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _cache = cache;
    }

    /// <summary>
    /// Handles the request to retrieve transaction metadata from the API cache.
    /// </summary>
    /// <param name="request">The request object containing the transaction hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the metadata or an error</returns>
    public async Task<Result<Metadata?>> Handle(GetApiMetadataRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TxHash))
        {
            _logger.LogError("Transaction hash is required");
            return Result.Fail<Metadata?>("Transaction hash is required");
        }

        var cacheKey = string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash);
        var cacheResult = _cache.TryGetValue(cacheKey, out TransactionMetadataWrapper transactionMetadataWrapper);
        
        if (!cacheResult || transactionMetadataWrapper == null)
        {
            _logger.LogDebug($"Transaction metadata for hash {request.TxHash} not found in cache");
            return Result.Ok<Metadata?>(null);
        }

        try
        {
            var transactionMetadata = transactionMetadataWrapper.transactionJson;
            var metadata = JsonSerializer.Deserialize<TransactionModel>(transactionMetadata);

            if (metadata == null)
            {
                _logger.LogWarning($"Failed to deserialize transaction metadata for hash {request.TxHash}");
                return Result.Fail<Metadata?>("Failed to deserialize transaction metadata");
            }

            return Result.Ok<Metadata?>(new Metadata
            {
                bytes = new byte[32], // Placeholder bytes
                json = transactionMetadata,
                id = 0,
                key = _appSettings.MetadataKey,
                tx_id = 0
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"JSON parsing error for transaction {request.TxHash}: {ex.Message}");
            return Result.Fail<Metadata?>($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error processing transaction {request.TxHash}: {ex.Message}");
            return Result.Fail<Metadata?>($"Unexpected error: {ex.Message}");
        }
    }
}