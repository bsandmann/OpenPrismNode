namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionWithPrismMetadataForBlockNo;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using GetApiTransactionIdsForBlock;
using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;
using Services;

/// <summary>
/// Retrieves all transaction IDs contained within a specific block from the Blockfrost API.
/// </summary>
public class GetApiTransactionWithPrismMetadataForBlockNoHandler : IRequestHandler<GetApiTransactionsWithPrismMetadataForBlockNoRequest, Result<List<Transaction>>>
{
    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetApiTransactionWithPrismMetadataForBlockNoHandler> _logger;
    private readonly AppSettings _appSettings;
    private readonly IAppCache _cache;


    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionIdsForBlockHandler"/> class.
    /// </summary>
    public GetApiTransactionWithPrismMetadataForBlockNoHandler(
        IMediator mediator,
        IHttpClientFactory httpClientFactory,
        ILogger<GetApiTransactionWithPrismMetadataForBlockNoHandler> logger,
        IOptions<AppSettings> appSettings,
        IAppCache cache
    )
    {
        _mediator = mediator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appSettings = appSettings.Value;
        _cache = cache;
    }


    public async Task<Result<List<Transaction>>> Handle(GetApiTransactionsWithPrismMetadataForBlockNoRequest request, CancellationToken cancellationToken)
    {
        // Get all the transactionid for the specific block
        var transactionIds = await _mediator.Send(new GetApiTransactionIdsForBlockRequest(request.BlockNo), cancellationToken);
        if (transactionIds.IsFailed)
        {
            return transactionIds.ToResult();
        }

        var transactions = new List<Transaction>();
        foreach (var transactionid in transactionIds.Value)
        {
            var transactionResults = await _mediator.Send(new GetApiTransactionRequest(transactionid, request.BlockNo, request.CurrentApiBlockTip), cancellationToken);
            if (transactionResults.IsFailed)
            {
                return transactionResults.ToResult();
            }

            if (transactionResults.Value != null)
            {
                transactions.Add(transactionResults.Value);
            }
        }

        return Result.Ok(transactions);
    }
}