using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Commands.ResolveDid;
using OpenPrismNode.Core.Commands.ResolveDid.Transform;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Models.DidDocument;
using OpenPrismNode.Sync.Services;
using Polly;

public class IngestionService : IIngestionService
{
    private readonly IMediator _mediator;
    private readonly ILogger<IngestionService> _logger;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public IngestionService(
        IMediator mediator,
        ILogger<IngestionService> logger,
        IOptions<AppSettings> appSettings,
        IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _logger = logger;
        _appSettings = appSettings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public Task Ingest(string didIdentifier, LedgerType requestLedger)
    {
        if (_appSettings.IngestionEndpoint is not null && requestLedger != LedgerType.InMemory)
        {
            // Start the background task
            _ = Task.Run(async () =>
            {
                try
                {
                    await IngestInBackground(didIdentifier, requestLedger);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An unhandled exception occurred while ingesting DID {DidIdentifier}. Error: {ErrorMessage}",
                        didIdentifier,
                        ex.Message);
                }
            });
        }

        // Return immediately
        return Task.CompletedTask;
    }

    private async Task IngestInBackground(string didIdentifier, LedgerType requestLedger)
    {
        var resolutionResult = await PrepareResolutionResult(didIdentifier, requestLedger);
        if (resolutionResult == null)
        {
            // If preparation fails, log and exit
            _logger.LogError("Failed to prepare resolution result for DID {DidIdentifier}", didIdentifier);
            return;
        }

        var client = _httpClientFactory.CreateClient("Ingestion");
        var retryPolicy = ResiliencePolicies.GetRetryPolicy(_logger);

        try
        {
            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(resolutionResult),
                    Encoding.UTF8,
                    "application/json");

                return await client.PostAsync("/", content);
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to ingest DID {DidIdentifier} after retries. Status Code: {StatusCode}",
                    didIdentifier,
                    response.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully ingested DID {DidIdentifier} after retries",
                    didIdentifier);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to ingest DID {DidIdentifier} after retries. Error: {ErrorMessage}",
                didIdentifier,
                ex.Message);
        }
    }

    private async Task<DidResolutionResult?> PrepareResolutionResult(string didIdentifier, LedgerType requestLedger)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var resolveResult = await _mediator.Send(new ResolveDidRequest(requestLedger, didIdentifier));
        if (resolveResult.IsFailed)
        {
            _logger.LogError(
                "Failed to resolve DID {DidIdentifier} for ledger {RequestLedger}. Error: {Error}",
                didIdentifier,
                requestLedger,
                resolveResult.Errors.First().Message);
            return null;
        }

        stopWatch.Stop();
        var includeNetworkIdentifier = true;
        var didDocument = TransformToDidDocument.Transform(
            resolveResult.Value.InternalDidDocument,
            requestLedger,
            includeNetworkIdentifier,
            showMasterAndRevocationKeys: false);

        DateTime? nextUpdate = null;

        var resolutionResult = new DidResolutionResult()
        {
            Context = DidResolutionResult.DidResolutionResultContext,
            DidDocument = didDocument,
            DidDocumentMetadata = TransformToDidDocumentMetadata.Transform(
                resolveResult.Value.InternalDidDocument,
                requestLedger,
                nextUpdate,
                includeNetworkIdentifier,
                false),
            DidResolutionMetadata = new DidResolutionMetadata()
            {
                ContentType = DidResolutionHeader.ApplicationLdJsonProfile,
                Retrieved = DateTime.UtcNow,
                Duration = stopWatch.ElapsedMilliseconds > 0 ? stopWatch.ElapsedMilliseconds : null
            }
        };

        if (resolutionResult.DidDocumentMetadata.Deactivated == true)
        {
            // When deactivated, return an empty DID Document
            resolutionResult.DidDocument = new DidDocument();
        }

        return resolutionResult;
    }
}
