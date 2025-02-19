namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Common;
using Core.Commands.GetMostRecentBlock;
using Core.Models;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Models;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;
using Sync.Commands.GetPostgresBlockTip;

/// <inheritdoc />
[ApiController]
public class SyncController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SyncController> _logger;
    private readonly BackgroundSyncService _backgroundSyncService;
    private IMediator _mediator;

    /// <inheritdoc />
    public SyncController(IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<SyncController> logger, BackgroundSyncService backgroundSyncService, IMediator mediator)
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
        _mediator = mediator;
    }

    /// <summary>
    /// Force the automatic sync service to stop.
    /// </summary>
    /// <remarks>
    /// The service will be paused and no further syncing tasks will be performed until manually restarted.
    /// </remarks>
    /// <returns>An ActionResult indicating the result of the operation</returns>
    /// <response code="200">The sync service has been successfully stopped</response>
    /// <response code="401">Unauthorized request</response>
    [ApiKeyOrAdminRoleAuthorizationAttribute]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("api/v{version:apiVersion=1.0}/sync/stop")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> StopSyncService()
    {
        await _backgroundSyncService.StopService();
        _logger.LogInformation("The automatic sync service is stopped");
        return Ok();
    }

    /// <summary>
    /// Force the automatic sync service to restart.
    /// </summary>
    /// <remarks>
    /// The service will resume automatic syncing tasks immediately upon restart.
    /// This endpoint can be used to manually trigger a restart of the sync service,
    /// which may be useful after a manual stop or in case of unexpected service interruptions.
    /// </remarks>
    /// <returns>An ActionResult indicating the result of the operation</returns>
    /// <response code="200">The sync service has been successfully restarted</response>
    /// <response code="401">Unauthorized request</response>
    [ApiKeyOrAdminRoleAuthorizationAttribute]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("api/v{version:apiVersion=1.0}/sync/start")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> RestartSyncService()
    {
        await _backgroundSyncService.RestartServiceAsync();
        _logger.LogInformation($"The automatic sync service has been restarted");
        return Ok();
    }


    [ApiKeyOrAdminRoleAuthorizationAttribute]
    [HttpGet("api/v{version:apiVersion=1.0}/sync/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ApiVersion("1.0")]
    public ActionResult<SyncStatusModel> GetSyncStatus()
    {
        // Expose the current state of the background sync service
        var status = new SyncStatusModel(
            IsRunning: _backgroundSyncService.isRunning,
            IsLocked: _backgroundSyncService.isLocked
        );

        return Ok(status);
    }

    [ApiKeyOrUserRoleAuthorization]
    [HttpGet("api/v{version:apiVersion=1.0}/sync/progress/{ledger}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ApiVersion("1.0")]
    public async Task<ActionResult<SyncProgressModel>> GetSyncProgress(string ledger)
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

        var postgresSqlTip = await _mediator.Send(new GetPostgresBlockTipRequest());
        if (postgresSqlTip.IsFailed)
        {
            var mesage = $"Cannot get the postgres tip (dbSync) for syncing {_appSettings.PrismLedger.Name}: {postgresSqlTip.Errors.First().Message}";
            _logger.LogCritical(mesage);
            return BadRequest(mesage);
        }

        var mostRecentBlockResult = await _mediator.Send(new GetMostRecentBlockRequest(ledgerType));
        if (mostRecentBlockResult.IsFailed)
        {
            var message = mostRecentBlockResult.Errors.First().Message;
            _logger.LogCritical(message);
            return BadRequest(message);
        }

        var progess = new SyncProgressModel(postgresSqlTip.Value.block_no, mostRecentBlockResult.Value.BlockHeight);
        return Ok(progess
        );
    }
}