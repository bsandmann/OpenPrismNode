using LazyCache;
using MediatR;
using Moq;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.DeleteLedger;
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

        // this._mediatorMock.Setup(p => p.Send(It.IsAny<UpdateDidDocumentMetadataRequest>(), It.IsAny<CancellationToken>()))
        //     .Returns(async (UpdateDidDocumentMetadataRequest request, CancellationToken token) => await this._updateDidDocumentMetadataHandler.Handle(request, token));
    }

    public void Dispose()
        => this.Fixture.Cleanup();
}