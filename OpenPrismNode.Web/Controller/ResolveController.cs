namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Core.Commands.ResolveDid;
using Core.Commands.ResolveDid.Transform;
using Core.Parser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

    // /// <summary>
    // /// Resolves the DID in the raw format as stored in the database of the open-prism-node
    // /// </summary>
    // /// <returns></returns>
    // [HttpGet("api/v{version:apiVersion}/resolvelegacy")]
    // [ApiVersion("1.0")]
    // public async Task<ActionResult> Resolve([FromQuery] string didIdentifier, [FromQuery] string ledger)
    // {
    //     if (string.IsNullOrEmpty(ledger))
    //     {
    //         return BadRequest("The ledger must be provided, e.g 'preprod' or 'mainnet'");
    //     }
    //
    //     if (!DidUrlParser.TryParse(didIdentifier, out var parsedDidUrl))
    //     {
    //        return BadRequest("The legacy resolver just pro");
    //     }
    //
    //     var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
    //     if (!isParseable)
    //     {
    //         return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
    //     }
    //
    //     if (string.IsNullOrWhiteSpace(didIdentifier))
    //     {
    //         return BadRequest("The didIdentifier must be provided");
    //     }
    //
    //     var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgerType, didIdentifier, null, null, null));
    //     if (resolveResult.IsFailed)
    //     {
    //         _logger.LogError($"Unable to resolver {didIdentifier} for ledger {ledger}. Error: {resolveResult.Errors.FirstOrDefault()?.Message}");
    //         return BadRequest(resolveResult.Errors.FirstOrDefault());
    //     }
    //     else if (resolveResult.Value is null)
    //     {
    //         return NotFound();
    //     }
    //
    //     return Ok(resolveResult.Value);
    // }

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

            var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgerQueryType, parsedDid.MethodSpecificId, null, null, null));
            if (resolveResult.IsFailed)
            {
                _logger.LogError($"Unable to resolver {parsedDid.MethodSpecificId} for ledger {ledger}. Error: {resolveResult.Errors.FirstOrDefault()?.Message}");
                return BadRequest(resolveResult.Errors.FirstOrDefault());
            }
            else if (resolveResult.Value is null)
            {
                return StatusCode(401);
            }

            var didDocument = TransformToDidDocument.Transform(resolveResult.Value.InternalDidDocument, ledgerQueryType, includeNetworkIdentifier: false, showMasterAndRevocationKeys: false);
            return Ok(didDocument);


            // // Perform DID resolution
            // var result = ResolveDidDocument(did, options);
            //
            // // Check if DID is deactivated
            // if (result.Deactivated)
            // {
            //     return StatusCode(410);
            // }
            //
            // // Check desired representation
            // string acceptHeader = Request.Headers["Accept"];
            // if (string.IsNullOrEmpty(acceptHeader) || acceptHeader == "application/did+ld+json")
            // {
            //     return Ok(result.DidDocument);
            // }
            // else if (acceptHeader == "application/ld+json;profile=\"https://w3id.org/did-resolution\"")
            // {
            //     var resolutionResult = new
            //     {
            //         didDocument = result.DidDocument,
            //         didResolutionMetadata = result.ResolutionMetadata,
            //         didDocumentMetadata = result.DocumentMetadata
            //     };
            //     return Ok(resolutionResult);
            // }
            // else
            // {
            //     return StatusCode(406, new { error = "representationNotSupported" });
            // }
        }
        // catch (NotFoundException)
        // {
        //     return NotFound(new { error = "notFound" });
        // }
        catch (Exception)
        {
            return StatusCode(500, new { error = "internalError" });
        }

        return null;
    }
}