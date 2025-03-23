namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Commands.ApiSync.GetApiBlockTip;
using Commands.ApiSync.GetApiTransactionMetadata;
using Commands.ApiSync.GetApiTransactionWithPrismMetadataForBlockNo;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Abstractions;

/// <summary>
/// Implementation of the ITransactionProvider interface that retrieves data from the Blockfrost API.
/// </summary>
public class BlockfrostTransactionProvider : ITransactionProvider
{

    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlockfrostTransactionProvider> _logger;
    private readonly AppSettings _appSettings;

    // PRISM metadata key
    public BlockfrostTransactionProvider(
        IMediator mediator,
        IHttpClientFactory httpClientFactory,
        ILogger<BlockfrostTransactionProvider> logger,
        IOptions<AppSettings> appSettings)
    {
        _mediator = mediator;
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
    public async Task<Result<List<Payment>>> GetPaymentDataFromTransaction(int txId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<Result<List<Transaction>>> GetTransactionsWithPrismMetadataForBlockId(int blockId, int blockNo, CancellationToken cancellationToken = default)
    {
        // Use the new API handler to get the block tip
        var results = await _mediator.Send(new GetApiTransactionsWithPrismMetadataForBlockNoRequest(blockNo), cancellationToken);
        if (results.IsFailed)
        {
            return results.ToResult();
        }
        return Result.Ok(results.Value);
    }
}
