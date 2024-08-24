namespace OpenPrismNode.Web.Controller;

using Core.Commands.ResolveDid;
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

    /// <inheritdoc />
    public ResolveController(IMediator mediator, ILogger<ResolveController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the DID
    /// </summary>
    /// <returns></returns>
    [HttpGet("api/resolve")]
    public async Task<ActionResult> Resolve([FromQuery] string didIdentifier, [FromQuery] string ledger)
    {
        if (string.IsNullOrEmpty(ledger))
        {
            return BadRequest("The ledger must be provided, e.g 'preprod' or 'mainnet'");
        }

        var isParseable = Enum.TryParse<LedgerType>("cardano" + ledger, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        if (string.IsNullOrWhiteSpace(didIdentifier))
        {
            return BadRequest("The didIdentifier must be provided");
        }

        var resolveResult = await _mediator.Send(new ResolveDidRequest(ledgerType, didIdentifier, null, null, null));
        if (resolveResult.IsFailed)
        {
            _logger.LogError($"Unable to resolver {didIdentifier} for ledger {ledger}. Error: {resolveResult.Errors.FirstOrDefault()?.Message}");
            return BadRequest(resolveResult.Errors.FirstOrDefault());
        }
        else if (resolveResult.Value is null)
        {
            return NotFound();
        }

        return Ok(resolveResult.Value);
    }
}