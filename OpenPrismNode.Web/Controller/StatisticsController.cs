namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Common;
using Core.Commands.CreateCardanoWallet;
using Core.Commands.GetDidList;
using Core.Commands.GetOperationStatus;
using Core.Commands.GetStakeAddressesForDay;
using Core.Commands.GetWallet;
using Core.Commands.GetWallets;
using Core.Commands.GetWalletTransactions;
using Core.Commands.RestoreWallet;
using Core.Commands.Withdrawal;
using Core.Commands.WriteTransaction;
using Core.Models;
using Core.Services;
using FluentResults;
using Google.Protobuf;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models;
using OpenPrismNode.Web;
using OpenPrismNode.Core.Common;

/// <inheritdoc />
[ApiController]
public class StatisticsController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly IMediator _mediator;


    /// <inheritdoc />
    public StatisticsController(IMediator mediator, IOptions<AppSettings> appSettings, ILogger<StatisticsController> logger)
    {
        _appSettings = appSettings.Value;
        _mediator = mediator;
    }

    [ApiKeyOrUserRoleAuthorization]
    [HttpGet("api/v{version:apiVersion=1.0}/statistics/{ledger}/stakeaddresses/{day}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetStakeAddresses(string ledger, DateOnly day)
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

        var getStakeAddressesForDayResult = await _mediator.Send(new GetStakeAddressesForDayRequest(ledgerType, day));
        if (getStakeAddressesForDayResult.IsFailed)
        {
            return BadRequest(getStakeAddressesForDayResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(getStakeAddressesForDayResult.Value);
    }
    
    // [ApiKeyOrUserRoleAuthorization]
    [HttpGet("api/v{version:apiVersion=1.0}/statistics/{ledger}/dids")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetDidList(string ledger)
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

        var getDidListResult = await _mediator.Send(new GetDidListRequest { Ledger = ledgerType });
        if (getDidListResult.IsFailed)
        {
            return BadRequest(getDidListResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(getDidListResult.Value);
    }
}