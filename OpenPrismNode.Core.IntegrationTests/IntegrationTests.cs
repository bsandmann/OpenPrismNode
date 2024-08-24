using LazyCache;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;
using OpenPrismNode.Core.Commands.DeleteEpoch;
using OpenPrismNode.Core.Commands.DeleteLedger;
using OpenPrismNode.Core.Commands.GetBlockByBlockHash;
using OpenPrismNode.Core.Commands.GetBlockByBlockHeight;
using OpenPrismNode.Core.Commands.GetEpoch;
using OpenPrismNode.Core.Commands.GetMostRecentBlock;
using OpenPrismNode.Core.IntegrationTests;

[Collection("TransactionalTests")]
public partial class IntegrationTests : IDisposable
{
    private TransactionalTestDatabaseFixture Fixture { get; }
    readonly IAppCache _mockedCache;
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



    public IntegrationTests(TransactionalTestDatabaseFixture fixture)
    {
        this.Fixture = fixture;
        this._context = this.Fixture.CreateContext();

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

        // this._mediatorMock.Setup(p => p.Send(It.IsAny<UpdateDidDocumentMetadataRequest>(), It.IsAny<CancellationToken>()))
        //     .Returns(async (UpdateDidDocumentMetadataRequest request, CancellationToken token) => await this._updateDidDocumentMetadataHandler.Handle(request, token));
    }

    public void Dispose()
        => this.Fixture.Cleanup();
}