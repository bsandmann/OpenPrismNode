namespace OpenPrismNode.Web.Controller;

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
    /// Force the automatic sync service to stop
    /// </summary>
    /// <returns></returns>
    [HttpPost("api/sync/stop")]
    public async Task<ActionResult> StopSyncService()
    {
        var hasAuthorization = _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("authorization", out StringValues authorization);
        if (!hasAuthorization || authorization.FirstOrDefault() == null || string.IsNullOrWhiteSpace(authorization) || !authorization.First()!.Equals(_appSettings.AuthorizationKey, StringComparison.InvariantCultureIgnoreCase))
        {
            return StatusCode(401);
        }

        await _backgroundSyncService.StopService();
        _logger.LogInformation($"The automatic sync service is stopped");
        return Ok();
    }

    /// <summary>
    /// Force the automatic sync service to stop
    /// </summary>
    /// <returns></returns>
    [HttpPost("api/sync/restart")]
    public async Task<ActionResult> RestartSyncService()
    {
        var hasAuthorization = _httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("authorization", out StringValues authorization);
        if (!hasAuthorization || authorization.FirstOrDefault() == null || string.IsNullOrWhiteSpace(authorization) || !authorization.First()!.Equals(_appSettings.AuthorizationKey, StringComparison.InvariantCultureIgnoreCase))
        {
            return StatusCode(401);
        }

        await _backgroundSyncService.RestartServiceAsync();
        _logger.LogInformation($"The automatic sync service has been restarted");
        return Ok();
    }
}