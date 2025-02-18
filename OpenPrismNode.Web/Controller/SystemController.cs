namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Common;
using Core.Commands.DeleteBlock;
using Core.Commands.DeletedOrphanedAddresses;
using Core.Commands.DeleteEmptyEpoch;
using Core.Commands.DeleteEpoch;
using Core.Commands.GetMostRecentBlock;
using FluentResults;
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
public class SystemController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BackgroundSyncService _backgroundSyncService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<LedgersController> _logger;

    /// <inheritdoc />
    public SystemController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<LedgersController> logger, BackgroundSyncService backgroundSyncService)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
    }

    /// <summary>
    /// Health check endpoint to verify the service is running.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [HttpGet("api/v{version:apiVersion=1.0}/system/health")]
    [ApiVersion("1.0")]
    public Task<ActionResult> HealthCheck()
    {
        return Task.FromResult<ActionResult>(Ok($"OpenPrismNode - Version {OpnVersion.GetVersion()}"));
    }
}