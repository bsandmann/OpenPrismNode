namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Core;
using Core.Commands.GetMaxBlockHeightForDateTime;
using Core.Commands.GetNextOperation;
using Core.Commands.GetOperationLedgerTime;
using Core.Commands.ResolveDid;
using Core.Commands.ResolveDid.Transform;
using Core.Models.DidDocument;
using Core.Parser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class ResolveController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ResolveController> _logger;
    private readonly IOptions<AppSettings> _appSettings;

    /// <inheritdoc />
    public ResolveController(IMediator mediator, IOptions<AppSettings> appSettings, ILogger<ResolveController> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _appSettings = appSettings;
    }

    [HttpGet("api/v{version:apiVersion}/identifiers/{did}")]
    [ApiVersion("1.0")]
    public async Task<IActionResult> ResolveDid(string did, [FromQuery] ResolutionOptions? options = null, [FromQuery] string? ledger = null)
    {
        try
        {
            if (string.IsNullOrEmpty(ledger))
            {
                ledger = _appSettings.Value.PrismLedger.Name;
            }

            var isParseableLedger = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerQueryType);
            if (!isParseableLedger)
            {
                return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
            }

            if (!DidUrlParser.TryParse(did, out var parsedDid))
            {
                return BadRequest(new { error = "invalidDid" });
            }

            if (!string.Equals(parsedDid.MethodName, "prism", StringComparison.InvariantCultureIgnoreCase))
            {
                return StatusCode(501, new { error = "methodNotSupported" });
            }

            if (!string.IsNullOrEmpty(parsedDid.Network))
            {
                var parsedDidLedger = Enum.TryParse<LedgerType>("cardano" + parsedDid.Network, ignoreCase: true, out var ledgerDidType);
                if (!parsedDidLedger)
                {
                    return StatusCode(501, new { error = "methodNotSupported" });
                }

                if (ledgerDidType != ledgerQueryType)
                {
                    return BadRequest("A valid network identifier (e.g. 'preprod', 'mainnet') must be provided either in the did, the ledger or the configuration");
                }
            }


            // TODO Check for long form or short form did

            //TODO expand the network configuration to multiple networks -> Check if the network is supported then

            if (options is not null)
            {
                if (!string.IsNullOrEmpty(options.VersionId) && options.VersionTime.HasValue)
                {
                    return BadRequest("VersionId and VersionTime are mutually exclusive.");
                }
            }

            string? acceptHeader = Request.Headers.Accept;
            var acceptedContentType = DidResolutionHeader.ParseAcceptHeader(acceptHeader);

            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            int? maxBlockHeight = null;
            int? maxBlockSequence = null;
            int? maxOperationSequence = null;
            if (options is not null && options.VersionTime.HasValue && options.VersionTime.Value < DateTime.UtcNow)
            {
                // VersionTime
                var maxBlockHeightResult = await _mediator.Send(new GetMaxBlockHeightForDateTimeRequest(ledgerQueryType, options.VersionTime.Value));
                if (maxBlockHeightResult.IsFailed)
                {
                    return StatusCode(500, maxBlockHeightResult.Errors.First().Message);
                }

                maxBlockHeight = maxBlockHeightResult.Value;
                maxBlockSequence = 10_000;
                maxOperationSequence = 10_000;
            }
            else if (options is not null && !string.IsNullOrWhiteSpace(options.VersionId))
            {
                var versionResult = await _mediator.Send(new GetOperationLedgerTimeRequest(options.VersionId, ledgerQueryType));
                if (versionResult.IsFailed)
                {
                    if (acceptedContentType == AcceptedContentType.DidResolutionResult)
                    {
                        return NotFound(CreateDidResolutionResultWithError(versionResult.Errors.FirstOrDefault().Message));
                    }

                    return NotFound(versionResult.Errors.FirstOrDefault().Message);
                }

                maxBlockHeight = versionResult.Value.LedgerTimeBlockHeight;
                maxBlockSequence = versionResult.Value.LedgerTimeBlockSequence;
                maxOperationSequence = versionResult.Value.LedgerTimeOperationSequence + 1;
            }

            var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgerQueryType, parsedDid.MethodSpecificId, maxBlockHeight, maxBlockSequence, maxOperationSequence));
            stopWatch.Stop();
            if (resolveResult.IsFailed)
            {
                _logger.LogError($"Unable to resolver {parsedDid.MethodSpecificId} for ledger {ledger}. Error: {resolveResult.Errors.FirstOrDefault()?.Message}");
                if (acceptedContentType == AcceptedContentType.DidResolutionResult)
                {
                    return NotFound(CreateDidResolutionResultWithError(resolveResult.Errors.FirstOrDefault().Message));
                }

                return NotFound(resolveResult.Errors.FirstOrDefault().Message);
            }
            else if (resolveResult.Value is null)
            {
                // Not found
                if (acceptedContentType == AcceptedContentType.DidResolutionResult)
                {
                    return NotFound(CreateDidResolutionResultWithError("notFound"));
                }

                return NotFound(new { error = "notFound" });
            }

            var didDocument = TransformToDidDocument.Transform(resolveResult.Value.InternalDidDocument, ledgerQueryType, includeNetworkIdentifier: false, showMasterAndRevocationKeys: false);

            switch (acceptedContentType)
            {
                case AcceptedContentType.DidDocument:
                    Response.ContentType = DidResolutionHeader.ApplicationDidLdJson;
                    if ((didDocument.VerificationMethod is null || didDocument.VerificationMethod is not null && !didDocument.VerificationMethod.Any()) &&
                        (didDocument.Service is null || didDocument.Service is not null && !didDocument.Service.Any()))
                    {
                        // Deactivated
                        return StatusCode(410, new { error = "deactivated" });
                    }

                    return Ok(didDocument);

                case AcceptedContentType.DidResolutionResult:
                    DateTime? nextUpdate = null;
                    if (options is not null && ((options.VersionTime.HasValue && options.VersionTime.Value < DateTime.Now) || !string.IsNullOrWhiteSpace(options.VersionId)))
                    {
                        var currentOperationHashString = resolveResult.Value.InternalDidDocument.VersionId;
                        var currentOperationHash = PrismEncoding.Base64ToByteArray(currentOperationHashString);
                        var nextOperation = await _mediator.Send(new GetNextOperationRequest(currentOperationHash, ledgerQueryType));
                        if (nextOperation.IsFailed)
                        {
                            return StatusCode(500, new { error = "internalError", message = nextOperation.Errors.First().Message });
                        }

                        if (nextOperation.Value.HasValue)
                        {
                            nextUpdate = DateTime.SpecifyKind(nextOperation.Value.Value, DateTimeKind.Utc);
                        }
                    }

                    Response.ContentType = DidResolutionHeader.ApplicationLdJsonProfile;
                    var resolutionResult = new DidResolutionResult()
                    {
                        Context = DidResolutionResult.DidResolutionResultContext,
                        DidDocument = didDocument,
                        DidDocumentMetadata = TransformToDidDocumentMetadata.Transform(resolveResult.Value.InternalDidDocument, ledgerQueryType, nextUpdate, includeNetworkIdentifier: false),
                        DidResolutionMetadata = new DidResolutionMetadata()
                        {
                            ContentType = DidResolutionHeader.ApplicationLdJsonProfile,
                            Retrieved = DateTime.UtcNow,
                            Duration = stopWatch.ElapsedMilliseconds > 0 ? stopWatch.ElapsedMilliseconds : null
                        }
                    };

                    if (resolutionResult.DidDocumentMetadata.Deactivated == true)
                    {
                        // When decativate we return for the DidDocument an empty json object
                        resolutionResult.DidDocument = new DidDocument();
                    }

                    return Ok(resolutionResult);

                case AcceptedContentType.Other:
                    return StatusCode(406, new { error = "representationNotSupported" });

                default:
                    return StatusCode(500, new { error = "internalError" });
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, new { error = "internalError", message = e.Message });
        }
    }

    private DidResolutionResult CreateDidResolutionResultWithError(string error)
    {
        return new DidResolutionResult()
        {
            Context = DidResolutionResult.DidResolutionResultContext,
            DidDocument = new DidDocument(),
            DidDocumentMetadata = null,
            DidResolutionMetadata = new DidResolutionMetadata()
            {
                ContentType = DidResolutionHeader.ApplicationLdJsonProfile,
                Error = error,
            }
        };
    }
}