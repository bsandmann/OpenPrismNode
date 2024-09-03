using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Commands.CreateAddresses;
using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.CreateStakeAddress;
using OpenPrismNode.Core.Commands.CreateTransaction;
using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateTransactionDeactivateDid;
using OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;
using OpenPrismNode.Core.Commands.CreateWalletAddress;
using OpenPrismNode.Core.Commands.DeleteEpoch;
using OpenPrismNode.Core.Commands.DeleteLedger;
using OpenPrismNode.Core.Commands.GetBlockByBlockHash;
using OpenPrismNode.Core.Commands.GetBlockByBlockHeight;
using OpenPrismNode.Core.Commands.GetEpoch;
using OpenPrismNode.Core.Commands.GetMostRecentBlock;
using OpenPrismNode.Core.Commands.ResolveDid;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.IntegrationTests;
using OpenPrismNode.Core.Services;
using OpenPrismNode.Sync.Commands.DecodeTransaction;
using OpenPrismNode.Sync.Commands.ParseTransaction;
using OpenPrismNode.Sync.Commands.ProcessBlock;
using OpenPrismNode.Sync.Commands.ProcessTransaction;

[Collection("TransactionalTests")]
public partial class IntegrationTests : IDisposable
{
    private TransactionalTestDatabaseFixture Fixture { get; }
    readonly IAppCache _mockedCache;
    readonly IOptions<AppSettings> _appSettingsOptions;
    private readonly DataContext _context;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CreateLedgerHandler _createLedgerHandler;
    private readonly CreateEpochHandler _createEpochHandler;
    private readonly CreateBlockHandler _createBlockHandler;
    private readonly DeleteLedgerHandler _deleteLedgerHandler;
    private readonly GetBlockByBlockHeightHandler _getBlockByBlockHeightHandler;
    private readonly GetBlockByBlockHashHandler _getBlockByBlockHashHandler;
    private readonly GetMostRecentBlockHandler _getMostRecentBlockHandler;
    private readonly GetEpochHandler _getEpochHandler;
    private readonly DeleteEpochHandler _deleteEpochHandler;
    private readonly CreateTransactionCreateDidHandler _createTransactionCreateDidHandler;
    private readonly CreateTransactionUpdateDidHandler _createTransactionUpdateDidHandler;
    private readonly CreateTransactionDeactivateDidHandler _createTransactionDeactivateDidHandler;
    private readonly CreateWalletAddressHandler _createWalletAddressHandler;
    private readonly CreateStakeAddressHandler _createStakeAddressHandler;
    private readonly IWalletAddressCache _walletAddressCache;
    private readonly IStakeAddressCache _stakeAddressCache;
    private readonly ResolveDidHandler _resolveDidHandler;
    private readonly ProcessBlockHandler _processBlockHandler;
    private readonly ProcessTransactionHandler _processTransactionHandler;
    private readonly DecodeTransactionHandler _decodeTransactionHandler;
    private readonly SwitchBranchHandler _switchBranchHandler;
    private readonly CreateAddressesHandler _createAddressesHandler;
    private readonly ParseTransactionHandler _parseTransactionHandler;
    private readonly CreateTransactionHandler _createTransactionHandler;

    public IntegrationTests(TransactionalTestDatabaseFixture fixture)
    {
        _walletAddressCache = new WalletAddressCache(100);
        _stakeAddressCache = new StakeAddressCache(100);


        this.Fixture = fixture;
        this._context = this.Fixture.CreateContext();
        this._appSettingsOptions = Options.Create(new AppSettings()
        {
            PrismLedger = new PrismLedger()
            {
                Name = "preprod"
            }
        });
        this._mediatorMock = new Mock<IMediator>();
        this._mockedCache = LazyCache.Testing.Moq.Create.MockedCachingService();
        this._createLedgerHandler = new CreateLedgerHandler(_context);
        this._createEpochHandler = new CreateEpochHandler(_context);
        this._deleteLedgerHandler = new DeleteLedgerHandler(_context, _mediatorMock.Object);
        this._createBlockHandler = new CreateBlockHandler(_context);
        this._getBlockByBlockHeightHandler = new GetBlockByBlockHeightHandler(_context);
        this._getBlockByBlockHashHandler = new GetBlockByBlockHashHandler(_context);
        this._getMostRecentBlockHandler = new GetMostRecentBlockHandler(_context);
        this._getEpochHandler = new GetEpochHandler(_context, _mockedCache);
        this._deleteEpochHandler = new DeleteEpochHandler(_context);
        this._createTransactionCreateDidHandler = new CreateTransactionCreateDidHandler(_context, Mock.Of<ILogger<CreateTransactionCreateDidHandler>>());
        this._createTransactionUpdateDidHandler = new CreateTransactionUpdateDidHandler(_context, Mock.Of<ILogger<CreateTransactionUpdateDidHandler>>());
        this._createTransactionDeactivateDidHandler = new CreateTransactionDeactivateDidHandler(_context, Mock.Of<ILogger<CreateTransactionDeactivateDidHandler>>());
        this._createWalletAddressHandler = new CreateWalletAddressHandler(_context, _walletAddressCache, Mock.Of<ILogger<CreateWalletAddressHandler>>());
        this._createStakeAddressHandler = new CreateStakeAddressHandler(_context, _stakeAddressCache, Mock.Of<ILogger<CreateStakeAddressHandler>>());
        this._resolveDidHandler = new ResolveDidHandler(_context);
        this._processBlockHandler = new ProcessBlockHandler(_mediatorMock.Object, _appSettingsOptions, Mock.Of<ILogger<ProcessBlockHandler>>());
        this._processTransactionHandler = new ProcessTransactionHandler(_mediatorMock.Object,Mock.Of<ILogger<ProcessTransactionHandler>>());
        this._decodeTransactionHandler = new DecodeTransactionHandler();
        this._switchBranchHandler = new SwitchBranchHandler(_context, _mediatorMock.Object);
        this._createAddressesHandler = new CreateAddressesHandler(_mediatorMock.Object);
        this._parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, new Sha256ServiceBouncyCastle(), new EcServiceBouncyCastle(), Mock.Of<ILogger<ParseTransactionHandler>>());
        this._createTransactionHandler = new CreateTransactionHandler(_mediatorMock.Object, new Sha256ServiceBouncyCastle());

    }

    public void Dispose()
        => this.Fixture.Cleanup();
}