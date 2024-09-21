namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Core.Commands.CreateCardanoWallet;
using Core.Commands.GetOperationStatus;
using Core.Commands.GetWallet;
using Core.Commands.RestoreWallet;
using Core.Commands.WriteTransaction;
using Core.Services;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class OperationsController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ICardanoWalletService _walletService;
    private readonly IMediator _mediator;


    /// <inheritdoc />
    public OperationsController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<LedgersController> logger, BackgroundSyncService backgroundSyncService, IHttpClientFactory httpClientFactory, ICardanoWalletService walletService)
    {
        _appSettings = appSettings.Value;
        _walletService = walletService;
        _mediator = mediator;
    }


    [HttpGet("api/v{version:apiVersion=1.0}/operations/{operationStatusIdHex}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetTransaction(string operationStatusIdHex)
    {
        //TODO try catch 
        var operationStatusId = PrismEncoding.HexToByteArray(operationStatusIdHex);

        var operationStatusResult = await _mediator.Send(new GetOperationStatusRequest(operationStatusId));
        if (operationStatusResult.IsFailed)
        {
            return BadRequest(operationStatusResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(new GetTransactionResponseModel()
        {
            OperationId = PrismEncoding.ByteArrayToHex(operationStatusResult.Value.OperationStatusId),
            OperationHash = PrismEncoding.ByteArrayToHex(operationStatusResult.Value.OperationHash),
            Status = operationStatusResult.Value.Status.ToString(),
            CreatedUtc = operationStatusResult.Value.CreatedUtc,
            LastUpdatedUtc = operationStatusResult.Value.LastUpdatedUtc
        });
    }
}