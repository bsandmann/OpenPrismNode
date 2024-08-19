namespace OpenPrismNode.Web.Controller;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Commands.DeleteLedger;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class DeleteController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BackgroundSyncService _backgroundSyncService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<DeleteController> _logger;

    /// <inheritdoc />
    public DeleteController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<DeleteController> logger, BackgroundSyncService backgroundSyncService)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
    }

    /// <summary>
    /// Deletes the complete Network (but not the full database) 
    /// Any automatic syncing or the execution of other tasks is diabled in the meantime
    /// </summary>
    /// <returns></returns>
    [HttpDelete("api/delete")]
    public async Task<ActionResult> Delete([FromQuery] string network)
    {
        var hasAuthorization = _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("authorization", out StringValues authorization);
        if (!hasAuthorization || authorization.FirstOrDefault() == null || string.IsNullOrWhiteSpace(authorization) || !authorization.First()!.Equals(_appSettings.AuthorizationKey, StringComparison.InvariantCultureIgnoreCase))
        {
            return StatusCode(401);
        }

        if (string.IsNullOrEmpty(network))
        {
            return BadRequest("The network must be provided, e.g 'preprod' or 'mainnet'");
        }

        _backgroundSyncService.StopService();
        _logger.LogInformation($"The automatic sync service is stopped. Restart the service after the deletion is completed if needed");
        _logger.LogInformation($"Deleting {network} ledger...");

        var isParseable = Enum.TryParse<LedgerType>(network, ignoreCase: true, out var ledgerType);
        if (!isParseable)
        {
            return BadRequest("The valid network identifier must be provided: 'preprod','mainnet', or 'inmemory'");
        }

        var result = await _mediator.Send(new DeleteLedgerRequest(ledgerType));

        if (result.IsFailed)
        {
            _logger.LogError($"Unable to delete Ledger for prism:{network}");
            _logger.LogError($"{result.Errors.FirstOrDefault()?.Message}");
            return BadRequest(result.Errors.FirstOrDefault());
        }

        _logger.LogInformation($"Deleting Ledger for prism:{network} completed");

        return Ok();
    }
}