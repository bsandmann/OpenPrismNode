namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Core.Commands.CreateCardanoWallet;
using Core.Commands.GetOperationStatus;
using Core.Commands.GetWallet;
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

    [HttpPost("api/v{version:apiVersion=1.0}/wallets")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateWallet([FromBody] CreateWalletRequestModel requestModel)
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
            WalletId = createWalletResult.Value.WalletId
        });
    }

    [HttpPost("api/v{version:apiVersion=1.0}/wallets/restore")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> RestoreWallet([FromBody] RestoreWalletRequestModel requestModel)
    {
        // TODO Authorization??
        var restoreWalletResult = await _mediator.Send(new RestoreCardanoWalletRequest()
        {
            Name = requestModel.Name,
            Mnemonic = requestModel.Mnemonic
        });
        if (restoreWalletResult.IsFailed)
        {
            return BadRequest(restoreWalletResult.Errors?.FirstOrDefault()?.Message);
        }

        return Ok(new RestoreWalletResponseModel()
        {
            WalletId = restoreWalletResult.Value.WalletId,
        });
    }

    [HttpGet("api/v{version:apiVersion=1.0}/wallets/{walletId}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetWallet(string walletId)
    {
        // TODO Authorization - problematic walletId
        var getWalletResult = await _mediator.Send(new GetWalletRequest()
        {
            WalletId = walletId
        });
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

    [HttpPost("api/v{version:apiVersion=1.0}/wallets/{walletId}/transactions")]
    [ApiVersion("1.0")]
    // [Consumes("application/octet-stream")]
    [Consumes("text/plain")]
    [Produces("application/json")]
    public async Task<ActionResult> ExecuteTransaction(string walletId)
    {
        using var reader = new StreamReader(Request.Body);
        var signedAtalaOperationAsBase64EncodedByteString = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(signedAtalaOperationAsBase64EncodedByteString))
        {
            return BadRequest("Input string is empty or null");
        }
        // Validate base64
        if (!IsValidBase64(signedAtalaOperationAsBase64EncodedByteString))
        {
            return BadRequest("Invalid base64 input");
        }

        // Attempt to decode base64 and parse protobuf
        try
        {
            var byteStringSignedAtalaOperation = PrismEncoding.Base64ToByteString(signedAtalaOperationAsBase64EncodedByteString);
            if (byteStringSignedAtalaOperation == null)
            {
                return BadRequest("Unable to decode base64 input");
            }

            var signedAtalaOperation = SignedAtalaOperation.Parser.ParseFrom(byteStringSignedAtalaOperation);
            if (signedAtalaOperation == null)
            {
                return BadRequest("Unable to parse SignedAtalaOperation");
            }

            var transactionResult = await _mediator.Send(new WriteTransactionRequest()
            {
                WalletId = walletId,
                SignedAtalaOperation = signedAtalaOperation
            });

            if (transactionResult.IsFailed)
            {
                return BadRequest(transactionResult.Errors?.FirstOrDefault()?.Message);
            }

            return Ok(PrismEncoding.ByteArrayToHex(transactionResult.Value.OperationStatusId));
        }
        catch (Exception ex)
        {
            return BadRequest("Error processing the input: " + ex.Message);
        }
    }


    [HttpGet("api/v{version:apiVersion=1.0}/wallets/{walletId}/transactions")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetTransactions(string walletId)
    {
        var transactionsResult = await _mediator.Send(new GetWalletTransactionsRequest()
        {
            WalletId = walletId,
        });
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
    
    [HttpPost("api/v{version:apiVersion=1.0}/wallets/{walletId}/withdrawal/{withdrawalAddress}")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> Withdrawal(string walletId, string withdrawalAddress)
    {
        var transactionResult = await _mediator.Send(new WithdrawalRequest()
        {
            WalletId = walletId,
            WithdrawalAddress = withdrawalAddress
        });
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