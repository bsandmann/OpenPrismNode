namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DecodeTransaction;
using FluentResults;
using GetApiTransactionByHash;
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
    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiTransactionHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IAppCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionHandler"/> class.
    /// </summary>
    public GetApiMetadataHandler(
        IMediator mediator,
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiTransactionHandler> logger,
        IOptions<AppSettings> appSettings,
        IAppCache cache)
    {
        _mediator = mediator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
        _cache = cache;
    }

    /// <summary>
    /// Handles the request to retrieve transaction metadata from the Blockfrost API.
    /// </summary>
    /// <param name="request">The request object containing the transaction hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the transaction data or an error</returns>
    public async Task<Result<Metadata?>> Handle(GetApiMetadataRequest request, CancellationToken cancellationToken)
    {
        var cacheResult = _cache.TryGetValue(string.Concat(CacheKeys.MetadataFromPrismTransaction, request.TxHash), out TransactionMetadataWrapper transactionMetadataWrapper);
        if (cacheResult)
        {
            var transactionMetadata = transactionMetadataWrapper.transactionJson;
            var metadata = JsonSerializer.Deserialize<TransactionModel>(transactionMetadata);

            if (metadata != null)
            {
                return Result.Ok<Metadata?>(new Metadata
                {
                    bytes = new byte[32],
                    json = transactionMetadata,
                    id = 0,
                    key = 0,
                    tx_id = 0
                });
            }
        }

        return Result.Fail("Transaction metadata could not be found. This should not happen.");
    }
}