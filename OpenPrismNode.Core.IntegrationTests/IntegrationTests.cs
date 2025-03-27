using LazyCache;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentResults;
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
using OpenPrismNode.Sync.Abstractions;
using OpenPrismNode.Sync.Commands.DbSync.GetTransactionsWithPrismMetadataForBlockId;
using OpenPrismNode.Sync.Commands.DecodeTransaction;
using OpenPrismNode.Sync.Commands.ParseTransaction;
using OpenPrismNode.Sync.Commands.ProcessBlock;
using OpenPrismNode.Sync.Commands.ProcessTransaction;
using OpenPrismNode.Sync.Commands.SwitchBranch;
using OpenPrismNode.Sync.Implementations.DbSync;
using OpenPrismNode.Sync.Services;

[Collection("TransactionalTests")]
public partial class IntegrationTests : IDisposable
{
    private TransactionalTestDatabaseFixture Fixture { get; }
    readonly IAppCache _mockedCache;
    readonly IOptions<AppSettings> _appSettingsOptions;
    readonly Mock<IIngestionService> _mockIngestionService;
    readonly IIngestionService _ingestionService;
    private readonly DataContext _context;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly IServiceProvider _serviceProviderMock;
    private readonly Mock<ITransactionProvider> _mockTransactionProvider;
    private readonly IBlockProvider _blockProvider;
    private readonly ITransactionProvider _transactionProvider;
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
        
        // Set up mocks
        this._mediatorMock = new Mock<IMediator>();
        this._mockIngestionService = new Mock<IIngestionService>();
        this._ingestionService = _mockIngestionService.Object;
        this._mockedCache = LazyCache.Testing.Moq.Create.MockedCachingService();
        this._mockTransactionProvider = new Mock<ITransactionProvider>();
        
        // Set up default behaviors for transaction providers
        SetupTransactionProvidersForBlock();
        
        // Initialize proper BlockProvider and TransactionProvider for use with SyncService
        this._blockProvider = new DbSyncBlockProvider(_mediatorMock.Object);
        this._transactionProvider = new DbSyncTransactionProvider(_mediatorMock.Object);
        
        // Create a mock service provider that returns the test context
        _serviceProviderMock = Mock.Of<IServiceProvider>(sp => 
            sp.GetService(typeof(DataContext)) == _context);
            
        // Create a mock service scope that returns our mocked service provider
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock);
        
        // Create a mock service scope factory that returns our mocked service scope
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_serviceScopeMock.Object);
        
        // Initialize handlers with the mocked service scope factory
        this._createLedgerHandler = new CreateLedgerHandler(_serviceScopeFactoryMock.Object);
        this._createEpochHandler = new CreateEpochHandler(_serviceScopeFactoryMock.Object);
        this._deleteLedgerHandler = new DeleteLedgerHandler(_serviceScopeFactoryMock.Object, _mediatorMock.Object);
        this._createBlockHandler = new CreateBlockHandler(_serviceScopeFactoryMock.Object);
        this._getBlockByBlockHeightHandler = new GetBlockByBlockHeightHandler(_serviceScopeFactoryMock.Object);
        this._getBlockByBlockHashHandler = new GetBlockByBlockHashHandler(_serviceScopeFactoryMock.Object);
        this._getMostRecentBlockHandler = new GetMostRecentBlockHandler(_serviceScopeFactoryMock.Object);
        this._getEpochHandler = new GetEpochHandler(_serviceScopeFactoryMock.Object, _mockedCache);
        this._deleteEpochHandler = new DeleteEpochHandler(_serviceScopeFactoryMock.Object);
        this._createTransactionCreateDidHandler = new CreateTransactionCreateDidHandler(
            _serviceScopeFactoryMock.Object, 
            Mock.Of<ILogger<CreateTransactionCreateDidHandler>>());
        this._createTransactionUpdateDidHandler = new CreateTransactionUpdateDidHandler(
            _serviceScopeFactoryMock.Object);
        this._createTransactionDeactivateDidHandler = new CreateTransactionDeactivateDidHandler(
            _serviceScopeFactoryMock.Object);
        this._createWalletAddressHandler = new CreateWalletAddressHandler(
            _serviceScopeFactoryMock.Object, 
            _walletAddressCache);
        this._createStakeAddressHandler = new CreateStakeAddressHandler(
            _serviceScopeFactoryMock.Object, 
            _stakeAddressCache,
            Mock.Of<ILogger<CreateStakeAddressHandler>>());
        this._resolveDidHandler = new ResolveDidHandler(_serviceScopeFactoryMock.Object);
        this._processBlockHandler = new ProcessBlockHandler(
            _mediatorMock.Object, 
            _appSettingsOptions, 
            Mock.Of<ILogger<ProcessBlockHandler>>(),
            _mockTransactionProvider.Object);
        this._processTransactionHandler = new ProcessTransactionHandler(
            _mediatorMock.Object, 
            Mock.Of<ILogger<ProcessTransactionHandler>>(), 
            _appSettingsOptions, 
            _ingestionService,
            _mockTransactionProvider.Object);
        this._decodeTransactionHandler = new DecodeTransactionHandler();
        this._switchBranchHandler = new SwitchBranchHandler(_mediatorMock.Object, _serviceScopeFactoryMock.Object);
        this._createAddressesHandler = new CreateAddressesHandler(_mediatorMock.Object);
        this._parseTransactionHandler = new ParseTransactionHandler(_mediatorMock.Object, new Sha256ServiceBouncyCastle(), new EcServiceBouncyCastle(), Mock.Of<ILogger<ParseTransactionHandler>>());
        this._createTransactionHandler = new CreateTransactionHandler(_mediatorMock.Object, new Sha256ServiceBouncyCastle());
    }

    public void Dispose()
        => this.Fixture.Cleanup();
        
    /// <summary>
    /// Helper method to set up both the mediator mock and the transaction provider mock used by ProcessBlockHandler
    /// to ensure they both return the same PRISM transactions.
    /// </summary>
    /// <param name="transactions">Optional list of transactions to return. If null, returns an empty list.</param>
    protected void SetupTransactionProvidersForBlock(List<OpenPrismNode.Core.DbSyncModels.Transaction>? transactions = null)
    {
        var txList = transactions ?? new List<OpenPrismNode.Core.DbSyncModels.Transaction>();
        
        // Set up the mediator mock for GetTransactionsWithPrismMetadataForBlockIdRequest
        _mediatorMock.Setup(p => p.Send(It.IsAny<OpenPrismNode.Sync.Commands.DbSync.GetTransactionsWithPrismMetadataForBlockId.GetTransactionsWithPrismMetadataForBlockIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(txList));
            
        // Set up the transaction provider that's used directly by ProcessBlockHandler
        _mockTransactionProvider.Setup(p => p.GetTransactionsWithPrismMetadataForBlockId(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(txList));
            
        // Set up the transaction provider for GetMetadataFromTransaction, used by ProcessTransactionHandler
        _mockTransactionProvider.Setup(p => p.GetMetadataFromTransaction(
            It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new OpenPrismNode.Core.DbSyncModels.Metadata
            {
                id = 1,
                bytes = new byte[] { 1, 2, 3, 4 },
                json = "{\"v\":1,\"c\":[\"sample\"]}", // Empty valid PRISM metadata JSON
                key = 1,
                tx_id = 1
            }));
            
        // Set up the transaction provider for GetPaymentDataFromTransaction, used by ProcessTransactionHandler
        _mockTransactionProvider.Setup(p => p.GetPaymentDataFromTransaction(
            It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new OpenPrismNode.Core.DbSyncModels.Payment
            {
                Incoming = new List<OpenPrismNode.Core.DbSyncModels.Utxo>(),
                Outgoing = new List<OpenPrismNode.Core.DbSyncModels.Utxo>()
            }));
    }
}