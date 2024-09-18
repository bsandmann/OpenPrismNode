namespace OpenPrismNode.Web.Controller;

using Asp.Versioning;
using CardanoWalletApi;
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

    private static PostWalletResponse PostWalletResponse { get; set; }

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

    [HttpGet("api/v{version:apiVersion=1.0}/createWallet")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateWallet()
    {
        var nms = new CardanoSharp.Wallet.MnemonicService();
        var mnemonic = nms.Generate(24);
        var mnemonicList = mnemonic.Words.Split(" ").ToList();

        var cardanoWalletClient = new Client(_httpClient);
        var result = await cardanoWalletClient.PostWalletAsync(new ApiWalletPostData()
            {
                Name = "my test wallet",
                Address_pool_gap = 20,
                Passphrase = "my test passphrase",
                Mnemonic_sentence = mnemonicList
            }
        );
        PostWalletResponse = result;

        return Ok();
    }

    [HttpGet("api/v{version:apiVersion=1.0}/getWallet")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> GetWallet()
    {
        var cardanoWalletClient = new Client(_httpClient);
        try
        {
            var walletId = "c8c6cbda31400bd28310d404a37030c5f91bcf4d";
            GetWalletResponse result = await cardanoWalletClient.GetWalletAsync(walletId);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [HttpGet("api/v{version:apiVersion=1.0}/listAddresses")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> CreateAddresses()
    {
        var cardanoWalletClient = new Client(_httpClient);
        try
        {
            var walletId = "c8c6cbda31400bd28310d404a37030c5f91bcf4d";
            ICollection<AddressDetail> result = await cardanoWalletClient.ListAddressesAsync(walletId, State.Unused);
            var ff = 3;
            return Ok(new
            {
                result.ToList()[0].Id,
                result.ToList()[0].State,
                result.ToList()[0].Derivation_path,
                result.ToList()[0].AdditionalProperties,
            });
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }


    [HttpGet("api/v{version:apiVersion=1.0}/constructTransaction")]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult> ConstructTransaction()
    {
        var targetAddress = "addr_test1qpty9xc8d7kly7ua5rgtzrssc5eamzvhucjmjqvsgflykl84z33p0walfpkv9qnfermkfy5hf95dp3wrq42nmkmnpj6qjlmg7p";
        var fundingAddress = "addr_test1qpzhuswt9varmv5a3n8uhxnjn46sft876jnzm7mzmq6c7704z33p0walfpkv9qnfermkfy5hf95dp3wrq42nmkmnpj6qjzhfun";

        var cardanoWalletClient = new Client(_httpClient);
        try
        {
            var walletId = "c8c6cbda31400bd28310d404a37030c5f91bcf4d";
            ConstructedTransactionResult result = await cardanoWalletClient.ConstructTransactionAsync(walletId, new Body3()
            {
                Payments = new List<Payments>()
                {
                    new Payments()
                    {
                        Address = targetAddress,
                        Amount = new Amount()
                        {
                            Quantity = 1_000_000,
                            Unit = "lovelace" 
                        },
                        AdditionalProperties = null,
                        Assets = null
                    }
                },
                Vote = null,
                AdditionalProperties = null,
                Delegations = null,
                Encoding = "base64",
                Encrypt_metadata = null,
                Metadata = null,
                Mint_burn = null,
                Reference_policy_script_template = null,
                Validity_interval = null,
                Withdrawal = "self"
            });
            return Ok(result.Fee);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }
}