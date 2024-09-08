namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
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
    // public async Task<IActionResult> ResolveDid(string did, [FromQuery] ResolutionOptions? options, [FromQuery] string ledger)
    public async Task<IActionResult> ResolveDid(string did, [FromQuery] string? ledger = null)
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

            string? acceptHeader = Request.Headers.Accept;
            var acceptedContentType = DidResolutionHeader.ParseAcceptHeader(acceptHeader);

            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgerQueryType, parsedDid.MethodSpecificId, null, null, null));
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
                        return StatusCode(410);
                    }

                    return Ok(didDocument);

                case AcceptedContentType.DidResolutionResult:
                    Response.ContentType = DidResolutionHeader.ApplicationLdJsonProfile;
                    var resolutionResult = new DidResolutionResult()
                    {
                        Context = DidResolutionResult.DidResolutionResultContext,
                        DidDocument = didDocument,
                        DidDocumentMetadata = TransformToDidDocumentMetadata.Transform(resolveResult.Value.InternalDidDocument, ledgerQueryType, includeNetworkIdentifier: false),
                        DidResolutionMetadata = new DidResolutionMetadata()
                        {
                            ContentType = DidResolutionHeader.ApplicationLdJsonProfile,
                            Retrieved = DateTime.UtcNow,
                            Duration = stopWatch.ElapsedMilliseconds
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
            DidDocumentMetadata = new DidDocumentMetadata(),
            DidResolutionMetadata = new DidResolutionMetadata()
            {
                ContentType = DidResolutionHeader.ApplicationLdJsonProfile,
                Error = error
            }
        };
    }
}