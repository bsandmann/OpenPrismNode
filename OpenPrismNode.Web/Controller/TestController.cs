namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using Core.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Web;

/// <inheritdoc />
[ApiController]
public class TestController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ICardanoWalletService _walletService;


    /// <inheritdoc />
    public TestController(IMediator mediator, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, ILogger<DeleteController> logger, BackgroundSyncService backgroundSyncService, IHttpClientFactory httpClientFactory, ICardanoWalletService walletService)
    {
        _appSettings = appSettings.Value;
        _walletService = walletService;
    }

    [HttpPost("api/v{version:apiVersion=1.0}/createWallet")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateWallet()
    {
        var nms = new CardanoSharp.Wallet.MnemonicService();
        var mnemonic = nms.Generate(24);

        // Create a new wallet
        var mnemonicList = mnemonic.Words.Split(" ");
        var createWalletRequest = new CreateWalletRequest
        {
            Name = "My Test Wallet",
            Passphrase = "Secure Passphrase",
            MnemonicSentence = mnemonicList
        };

        var walletResponse = await _walletService.CreateWalletAsync(createWalletRequest);
        return Ok(walletResponse);
    }

    [HttpGet("api/v{version:apiVersion=1.0}/getWallet")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetWallet()
    {
        var walletId = "c8c6cbda31400bd28310d404a37030c5f91bcf4d";
        var result = await _walletService.GetWalletAsync(walletId);
        return Ok(result);
    }

    [HttpGet("api/v{version:apiVersion=1.0}/listAddresses")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateAddresses()
    {
        var walletId = "c8c6cbda31400bd28310d404a37030c5f91bcf4d";
        var result = await _walletService.ListAddressesAsync(walletId);
        return Ok(result);
    }


    [HttpPost("api/v{version:apiVersion=1.0}/executeTransaction")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> Full()
    {
        var walletId = "c8c6cbda31400bd28310d404a37030c5f91bcf4d";
        var toAddress = "addr_test1qpty9xc8d7kly7ua5rgtzrssc5eamzvhucjmjqvsgflykl84z33p0walfpkv9qnfermkfy5hf95dp3wrq42nmkmnpj6qjlmg7p";

        // Prepare payment (sending 1 ADA)
        var payment = new Payment
        {
            Address = toAddress,
            Amount = new Amount { Quantity = 1_000_000, Unit = "lovelace" } // 1 ADA = 1,000,000 lovelace
        };

        // Prepare metadata (assuming you already have the map structure)
        var metadata = new Dictionary<string, object>
        {
            ["0"] = new Dictionary<string, object> { ["string"] = "Hello, Cardano!" }
        };

        var passphrase = "my test passphrase";
        // Create and submit transaction
        var transactionId = await _walletService.CreateAndSubmitTransactionAsync(walletId, passphrase, payment, metadata);

        return Ok(transactionId);
    }
}