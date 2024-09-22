namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class SyncController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SyncController> _logger;
    private readonly BackgroundSyncService _backgroundSyncService;

    /// <inheritdoc />
    public SyncController(IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<SyncController> logger, BackgroundSyncService backgroundSyncService)
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
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
    [ApiKeyAdminAuthorization]
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
    [ApiKeyAdminAuthorization]
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
}