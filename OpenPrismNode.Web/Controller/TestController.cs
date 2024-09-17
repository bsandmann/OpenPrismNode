namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class TestController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BackgroundSyncService _backgroundSyncService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<DeleteController> _logger;
    private readonly HttpClient _httpClient;

    /// <inheritdoc />
    public TestController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<DeleteController> logger, BackgroundSyncService backgroundSyncService, IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings.Value;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
        _httpClient = httpClientFactory
            .CreateClient("CardanoWalletApi");
    }

    [HttpGet("api/v{version:apiVersion=1.0}/test")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> TestWriteOperation([FromQuery] string? ledger)
    {
        var cardanoWalletClient = new CardanoWalletApi.Client(_httpClient); 
        return Ok();
    }
}

