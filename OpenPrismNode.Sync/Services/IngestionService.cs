using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Commands.ResolveDid;
using OpenPrismNode.Core.Commands.ResolveDid.Transform;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Models.DidDocument;

namespace OpenPrismNode.Sync.Services;

public class IngestionService : IIngestionService, IDisposable
{
    private readonly IMediator _mediator;
    private readonly ILogger<IngestionService> _logger;
    private readonly AppSettings _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly Channel<DidResolutionResult> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _workerTask;

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

        // Initialize the channel and background worker
        _channel = Channel.CreateUnbounded<DidResolutionResult>();
        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _workerTask.Wait();
        _cts.Dispose();
    }

    public async Task Ingest(string didIdentifier, LedgerType requestLedger)
    {
        if (_appSettings.IngestionEndpoint is not null && requestLedger != LedgerType.InMemory)
        {
            DidResolutionResult? resolutionResult = await PrepareResolutionResult(didIdentifier, requestLedger);
            if (resolutionResult == null)
            {
                _logger.LogError("Failed to prepare resolution result for DID {DidIdentifier}", didIdentifier);
                return;
            }

            // Enqueue the resolutionResult into the channel
            await _channel.Writer.WriteAsync(resolutionResult);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_channel.Reader.TryRead(out var resolutionResult))
            {
                await ProcessItemAsync(resolutionResult, cancellationToken);
            }
        }
    }

    private async Task ProcessItemAsync(DidResolutionResult resolutionResult, CancellationToken cancellationToken)
    {
        var didIdentifier = resolutionResult.DidDocument?.Id;

        var client = _httpClientFactory.CreateClient("Ingestion");
        client.DefaultRequestHeaders.Add("Authorization", _appSettings.IngestionEndpointAuthorizationKey);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var content = new StringContent(
            JsonSerializer.Serialize(resolutionResult, jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Retry logic: keep trying until the request succeeds
        while (true)
        {
            try
            {
                var response = await client.PostAsync("", content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Successfully ingested DID {DidIdentifier}",
                        didIdentifier);
                    break; // Success, proceed to next item
                }
                else
                {
                    _logger.LogError(
                        "Failed to ingest DID {DidIdentifier}. Status Code: {StatusCode}",
                        didIdentifier,
                        response.StatusCode);

                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to ingest DID {DidIdentifier}. Error: {ErrorMessage}",
                    didIdentifier,
                    ex.Message);

                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
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
        var didDocument = TransformToDidDocument.Transform(
            resolveResult.Value.InternalDidDocument,
            requestLedger,
            includeNetworkIdentifier: true,
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
                includeNetworkIdentifier: true,
                isLongForm: false),
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
