namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Core.Commands.CreateCardanoWallet;
using Core.Commands.GetWalletState;
using Core.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class WalletController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ICardanoWalletService _walletService;
    private readonly IMediator _mediator;


    /// <inheritdoc />
    public WalletController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<DeleteController> logger, BackgroundSyncService backgroundSyncService, IHttpClientFactory httpClientFactory, ICardanoWalletService walletService)
    {
        _appSettings = appSettings.Value;
        _walletService = walletService;
        _mediator = mediator;
    }

    [HttpPost("api/v{version:apiVersion=1.0}/wallet")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateWallet([FromBody] CreateCardanoWalletRequest requestModel)
    {
        // TODO Authorization??
        var createWalletResult = await _mediator.Send(new CreateCardanoWalletRequest()
        {
            Name = requestModel.Name
        });
        if (createWalletResult.IsFailed)
        {
            return BadRequest(createWalletResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(new CreateWalletResponseModel()
        {
            Mnemonic = createWalletResult.Value.Mnemonic,
            WalletKey = createWalletResult.Value.WalletKey
        });
    }

    [HttpGet("api/v{version:apiVersion=1.0}/wallet")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetWallt([FromQuery] string walletKey)
    {
        // TODO Authorization - problematic walletkey
        var getWalletKeyResult = await _mediator.Send(new GetWalletStateRequest()
        {
            WalletKey = walletKey
        });
        if (getWalletKeyResult.IsFailed)
        {
            return BadRequest(getWalletKeyResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok();
    }
}