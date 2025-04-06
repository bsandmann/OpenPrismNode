namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Common;
using Core.Commands.CreateCardanoWallet;
using Core.Commands.GetOperationStatus;
using Core.Commands.GetWallet;
using Core.Commands.GetWallets;
using Core.Commands.GetWalletTransactions;
using Core.Commands.RestoreWallet;
using Core.Commands.Withdrawal;
using Core.Commands.WriteTransaction;
using Core.Services;
using FluentResults;
using Google.Protobuf;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class WalletsController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ICardanoWalletService _walletService;
    private readonly IMediator _mediator;


    /// <inheritdoc />
    public WalletsController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<LedgersController> logger, BackgroundSyncService backgroundSyncService, IHttpClientFactory httpClientFactory, ICardanoWalletService walletService)
    {
        _appSettings = appSettings.Value;
        _walletService = walletService;
        _mediator = mediator;
    }

    [ApiKeyOrUserRoleAuthorization]
    [HttpPost("api/v{version:apiVersion=1.0}/wallets")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateWallet([FromBody] CreateWalletRequestModel requestModel)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        var createWalletResult = await _mediator.Send(new CreateCardanoWalletRequest(requestModel.Name));
        if (createWalletResult.IsFailed)
        {
            return BadRequest(createWalletResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(new CreateWalletResponseModel()
        {
            Mnemonic = createWalletResult.Value.Mnemonic,
            WalletId = createWalletResult.Value.WalletId
        });
    }

    [ApiKeyOrUserRoleAuthorization]
    [HttpPost("api/v{version:apiVersion=1.0}/wallets/restore")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> RestoreWallet([FromBody] RestoreWalletRequestModel requestModel)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        var restoreWalletResult = await _mediator.Send(new RestoreCardanoWalletRequest(requestModel.Mnemonic, requestModel.Name));
        if (restoreWalletResult.IsFailed)
        {
            return BadRequest(restoreWalletResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(new RestoreWalletResponseModel()
        {
            WalletId = restoreWalletResult.Value.WalletId,
        });
    }

    [ApiKeyOrWalletUserRoleAuthorization]
    [HttpGet("api/v{version:apiVersion=1.0}/wallets/{walletId}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetWallet(string walletId)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        var getWalletResult = await _mediator.Send(new GetWalletRequest(walletId));
        if (getWalletResult.IsFailed)
        {
            return BadRequest(getWalletResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(new GetWalletResponseModel()
        {
            WalletId = walletId,
            Balance = getWalletResult.Value.Balance,
            FundingAddress = getWalletResult.Value.FundingAddress,
            SyncingComplete = getWalletResult.Value.SyncingComplete,
            SyncProgress = getWalletResult.Value.SyncProgress
        });
    }

    [ApiKeyOrAdminRoleAuthorizationAttribute]
    [HttpGet("api/v{version:apiVersion=1.0}/wallets/")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetWallets()
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        var getWalletsResult = await _mediator.Send(new GetWalletsRequest()
        {
        });
        if (getWalletsResult.IsFailed)
        {
            return BadRequest(getWalletsResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(getWalletsResult.Value.Select(p => new GetWalletResponseModel()
        {
            WalletId = p.WalletId,
            Balance = p.Balance,
            FundingAddress = p.FundingAddress,
            SyncingComplete = p.SyncingComplete,
            SyncProgress = p.SyncProgress
        }).ToList());
    }

    [ApiKeyOrWalletUserRoleAuthorization]
    [HttpPost("api/v{version:apiVersion=1.0}/wallets/{walletId}/transactions")]
    [ApiVersion("1.0")]
    [Consumes("text/plain", "application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> ExecuteTransaction(string walletId)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        using var reader = new StreamReader(Request.Body);
        var inputString = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(inputString))
        {
            return BadRequest("Input string is empty or null");
        }

        SignedAtalaOperation signedAtalaOperation;
        try
        {
            // Decide how to parse based on whether it's valid Base64
            if (IsValidBase64(inputString))
            {
                // Parse from Base64/Protobuf
                var byteStringSignedAtalaOperation = PrismEncoding.Base64ToByteString(inputString);
                if (byteStringSignedAtalaOperation == null)
                {
                    return BadRequest("Unable to decode base64 input");
                }

                signedAtalaOperation = SignedAtalaOperation.Parser.ParseFrom(byteStringSignedAtalaOperation);
            }
            else
            {
                // Parse from JSON
                signedAtalaOperation = SignedAtalaOperation.Parser.ParseJson(inputString);
            }

            if (signedAtalaOperation == null)
            {
                return BadRequest("Unable to parse SignedAtalaOperation from either base64 or JSON.");
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"Error parsing the input: {ex.Message}");
        }

        try
        {
            var transactionResult = await _mediator.Send(new WriteTransactionRequest(signedAtalaOperation, walletId));
            if (transactionResult.IsFailed)
            {
                return BadRequest(transactionResult.Errors?.FirstOrDefault()?.Message);
            }

            return Ok(PrismEncoding.ByteArrayToHex(transactionResult.Value.OperationStatusId));
        }
        catch (Exception ex)
        {
            return BadRequest($"Error processing the transaction: {ex.Message}");
        }
    }

    [ApiKeyOrWalletUserRoleAuthorization]
    [HttpGet("api/v{version:apiVersion=1.0}/wallets/{walletId}/transactions")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetTransactions(string walletId)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        var transactionsResult = await _mediator.Send(new GetWalletTransactionsRequest(walletId));
        if (transactionsResult.IsFailed)
        {
            return BadRequest(transactionsResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(transactionsResult.Value.Select(p => new GetWalletTransactionsResponseModel()
        {
            TransactionId = p.TransactionId,
            OperationStatusId = p.OperationStatusId is not null ? PrismEncoding.ByteArrayToHex(p.OperationStatusId) : null,
            OperationHash = p.OperationHash is not null ? PrismEncoding.ByteArrayToHex(p.OperationHash) : null,
            OperationType = p.OperationType.ToString(),
            Status = p.Status.ToString(),
            Fee = p.Fee,
        }));
    }

    [ApiKeyOrWalletUserRoleAuthorization]
    [HttpPost("api/v{version:apiVersion=1.0}/wallets/{walletId}/withdrawal/{withdrawalAddress}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> Withdrawal(string walletId, string withdrawalAddress)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.CardanoWalletApiEndpoint))
        {
            return BadRequest("CardanoWalletApiEndpoint is not conigured. Please check the settings before using this endpoint.");
        }

        var transactionResult = await _mediator.Send(new WithdrawalRequest(walletId, withdrawalAddress));
        if (transactionResult.IsFailed)
        {
            return BadRequest(transactionResult.Errors.FirstOrDefault().Message);
        }

        return Ok();
    }

    private bool IsValidBase64(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
        {
            return false;
        }

        try
        {
            PrismEncoding.Base64ToByteString(base64String);
            return true;
        }
        catch
        {
            return false;
        }
    }
}