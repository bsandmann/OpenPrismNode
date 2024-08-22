namespace OpenPrismNode.Sync.IntegrationTests.GetPaymentDataFromTransaction;

using Core.Common;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Sync.Commands.GetPaymentDataFromTransaction;
using OpenPrismNode.Core.Models;

public class GetPaymentDataFromTransactionTests
{
    private readonly TestNpgsqlConnectionFactory _testFactory;
    private readonly GetPaymentDataFromTransactionHandler _handler;
    private const int TestTxId = 1060; // Note: This is a known transaction ID in the test database. This value might be different with a new database!

    public GetPaymentDataFromTransactionTests()
    {
        _testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        var appSettings = new AppSettings { PrismLedger = new PrismLedger { DbSyncPostgresConnectionString = TestSetup.PostgreSqlConnectionString } };
        var options = Microsoft.Extensions.Options.Options.Create(appSettings);
        _handler = new GetPaymentDataFromTransactionHandler(options);
    }

    [Fact]
    public async Task Getting_payment_data_for_valid_transaction_succeeds()
    {
        // Arrange
        var request = new GetPaymentDataFromTransactionRequest(TestTxId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.Incoming.Should().NotBeNull();
        result.Value.Outgoing.Should().NotBeNull();

        result.Value.Incoming.Should().AllSatisfy(utxo =>
        {
            utxo.Index.Should().BeGreaterOrEqualTo(0);
            utxo.Value.Should().BeGreaterThan(0);
            utxo.WalletAddress.Should().NotBeNull();
            utxo.WalletAddress.WalletAddressString.Should().NotBeNullOrEmpty();
        });

        result.Value.Outgoing.Should().AllSatisfy(utxo =>
        {
            utxo.Index.Should().BeGreaterOrEqualTo(0);
            utxo.Value.Should().BeGreaterThan(0);
            utxo.WalletAddress.Should().NotBeNull();
            utxo.WalletAddress.WalletAddressString.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Getting_payment_data_for_nonexistent_transaction_returns_empty_payment()
    {
        // Arrange
        int nonexistentTxId = int.MaxValue; // Assume this transaction doesn't exist
        var request = new GetPaymentDataFromTransactionRequest(nonexistentTxId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.Incoming.Should().BeEmpty();
        result.Value.Outgoing.Should().BeEmpty();
    }

    [Fact]
    public async Task Getting_payment_data_preserves_utxo_order()
    {
        // Arrange
        var request = new GetPaymentDataFromTransactionRequest(TestTxId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Incoming.Should().BeInAscendingOrder(utxo => utxo.Index);
        result.Value.Outgoing.Should().BeInAscendingOrder(utxo => utxo.Index);
    }
}