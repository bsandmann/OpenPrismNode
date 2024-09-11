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
using Sync.Commands.ParseLongFormDid;
using ResolutionOptions = Models.ResolutionOptions;

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

    /// <summary>
    /// Resolves a Decentralized Identifier (DID) and returns the corresponding DID Document.
    /// </summary>
    /// <remarks>
    /// This endpoint implements the DID Resolution process as specified in the W3C DID Resolution specification.
    /// It supports resolution of PRISM DIDs, including long-form DIDs, and can return the DID Document in various formats.
    /// It supports the selection of a sepecific version either by versionId or versionTime.
    /// It allows the adding of the network-identifier in the output e.g. did:prism:prepord:123 if requested.
    /// It supports the following content types: application/did+ld+json, application/did+json, application/ld+json;profile="https://w3id.org/did-resolution".
    /// It does not support the 'noCache' option.
    /// It does not support dereferencing of DID URLs.
    /// It does not support cbor.
    /// </remarks>
    /// <param name="did">The DID to resolve</param>
    /// <param name="options">Optional resolution options including versionId, versionTime, and the inclusion of the network-identifier</param>
    /// <param name="ledger">Optional ledger to use for resolution: 'preprod', 'mainnet', or 'inmemory'. If not provided, defaults to the configuration setting.</param>
    /// <returns>The resolved DID Document or DID Resolution Result</returns>
    /// <response code="200">Successful resolution. Returns the DID Document or DID Resolution Result.</response>
    /// <response code="400">Bad request. Invalid DID format or incompatible resolution options.</response>
    /// <response code="404">Not found. The requested DID does not exist.</response>
    /// <response code="406">Not acceptable. The requested representation is not supported.</response>
    /// <response code="410">Gone. The DID has been deactivated.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    [HttpGet("api/v{version:apiVersion=1.0}/identifiers/{did}")]
    [ApiVersion("1.0")]
    [Produces("application/did+ld+json", "application/did+json", "application/ld+json;profile=\"https://w3id.org/did-resolution\"")]
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
                    if (parsedDid.Network.Length == 64 && parsedDid.MethodSpecificId.Length > 64)
                    {
                        // We assume that the network is the did-identifier because of the long-form did
                        // Rearranging the parsedDid to allow for longForm-DIDs
                        parsedDid.PrismLongForm = parsedDid.MethodSpecificId;
                        parsedDid.MethodSpecificId = parsedDid.Network;
                        parsedDid.Network = null;
                    }
                    else
                    {
                        return StatusCode(501, new { error = "methodNotSupported" });
                    }
                }
                else if (ledgerDidType != ledgerQueryType)
                {
                    return BadRequest("A valid network identifier (e.g. 'preprod', 'mainnet') must be provided either in the did, the ledger or the configuration");
                }

                // Rearranging the parsedDid to allow for longForm-DIDs
                if (!string.IsNullOrWhiteSpace(parsedDid.SubNetwork))
                {
                    parsedDid.PrismLongForm = parsedDid.MethodSpecificId;
                    parsedDid.MethodSpecificId = parsedDid.SubNetwork;
                    parsedDid.SubNetwork = null;
                }
            }

            //TODO expand the network configuration to multiple networks -> Check if the network is supported then

            if (options is not null)
            {
                if (!string.IsNullOrEmpty(options.VersionId) && options.VersionTime.HasValue)
                {
                    return BadRequest("VersionId and VersionTime are mutually exclusive.");
                }
            }

            InternalDidDocument? internalDidDocumentLongForm = null;
            if (parsedDid.PrismLongForm is not null)
            {
                var longFormDidDocumentResult = await _mediator.Send(new ParseLongFormDidRequest(parsedDid));
                if (longFormDidDocumentResult.IsFailed)
                {
                    return BadRequest(new { error = "invalidDid" });
                }

                internalDidDocumentLongForm = longFormDidDocumentResult.Value;
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
            if (resolveResult.IsFailed && internalDidDocumentLongForm is null)
            {
                _logger.LogError($"Unable to resolve did identifier '{parsedDid.MethodSpecificId}' for '{ledger}' ledger. Error: {resolveResult.Errors.FirstOrDefault()?.Message}");
                if (acceptedContentType == AcceptedContentType.DidResolutionResult)
                {
                    return NotFound(CreateDidResolutionResultWithError(resolveResult.Errors.FirstOrDefault().Message));
                }

                return NotFound(resolveResult.Errors.FirstOrDefault().Message);
            }
            else if (internalDidDocumentLongForm is null && resolveResult.ValueOrDefault is null)
            {
                // Not found
                if (acceptedContentType == AcceptedContentType.DidResolutionResult)
                {
                    return NotFound(CreateDidResolutionResultWithError("notFound"));
                }

                return NotFound(new { error = "notFound" });
            }

            var includeNetworkIdentifier = _appSettings.Value.PrismLedger.IncludeNetworkIdentifier;
            if (options is not null && options.IncludeNetworkIdentifier is not null && options.IncludeNetworkIdentifier != includeNetworkIdentifier)
            {
                includeNetworkIdentifier = options.IncludeNetworkIdentifier.Value;
            }

            var internalDidDocument = resolveResult.ValueOrDefault != null ? resolveResult.Value.InternalDidDocument : internalDidDocumentLongForm;
            var didDocument = TransformToDidDocument.Transform(internalDidDocument!, ledgerQueryType, includeNetworkIdentifier, showMasterAndRevocationKeys: false);

            switch (acceptedContentType)
            {
                case AcceptedContentType.DidDocumentJsonLd:
                    Response.ContentType = DidResolutionHeader.ApplicationDidLdJson;
                    if ((didDocument.VerificationMethod is null || didDocument.VerificationMethod is not null && !didDocument.VerificationMethod.Any()) &&
                        (didDocument.Service is null || didDocument.Service is not null && !didDocument.Service.Any()))
                    {
                        // Deactivated
                        return StatusCode(410, new { error = "deactivated" });
                    }

                    return Ok(didDocument);

                case AcceptedContentType.DidDocumentJson:
                    Response.ContentType = DidResolutionHeader.ApplicationDidJson;
                    if ((didDocument.VerificationMethod is null || didDocument.VerificationMethod is not null && !didDocument.VerificationMethod.Any()) &&
                        (didDocument.Service is null || didDocument.Service is not null && !didDocument.Service.Any()))
                    {
                        // Deactivated
                        return StatusCode(410, new { error = "deactivated" });
                    }

                    didDocument.Context = null;
                    return Ok(didDocument);

                case AcceptedContentType.DidResolutionResult:
                    DateTime? nextUpdate = null;
                    if (options is not null && ((options.VersionTime.HasValue && options.VersionTime.Value < DateTime.Now) || !string.IsNullOrWhiteSpace(options.VersionId)))
                    {
                        var currentOperationHashString = internalDidDocument!.VersionId;
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
                        DidDocumentMetadata = TransformToDidDocumentMetadata.Transform(internalDidDocument!, ledgerQueryType, nextUpdate, includeNetworkIdentifier, internalDidDocumentLongForm != null),
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