namespace OpenPrismNode.Sync.IntegrationTests.GetMetadataFromTransaction;

using Commands.DecodeTransaction;
using Core;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Sync.Commands.GetMetadataFromTransaction;
using OpenPrismNode.Core.Models;

public class GetMetadataFromTransactionTests
{
    private readonly TestNpgsqlConnectionFactory _testFactory;
    private readonly GetMetadataFromTransactionHandler _handler;
    private const int TestTxId = 1060; // Note: This is a known transaction ID in the test database. This value might be different with a new database!

    public GetMetadataFromTransactionTests()
    {
        _testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        _handler = new GetMetadataFromTransactionHandler(_testFactory);
    }

    [Fact]
    public async Task Getting_metadata_for_valid_transaction_and_key_succeeds()
    {
        // Arrange
        var request = new GetMetadataFromTransactionRequest(TestTxId, 21325);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.json.Should().NotBeNullOrEmpty();
      
    }

    [Fact]
    public async Task Getting_metadata_for_transaction_without_metadata_returns_failure()
    {
        // Arrange
        int txIdWithoutMetadata = 123;  // No related metadata in the test database
        var request = new GetMetadataFromTransactionRequest(txIdWithoutMetadata, 21325);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("Metadata not found");
    }

    [Fact]
    public async Task Getting_metadata_for_nonexistent_transaction_returns_failure()
    {
        // Arrange
        int nonexistentTxId = int.MaxValue; // Assume this transaction doesn't exist
        var request = new GetMetadataFromTransactionRequest(nonexistentTxId, 21325);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("Metadata not found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Getting_metadata_with_invalid_txId_returns_failure(int invalidTxId)
    {
        // Arrange
        var request = new GetMetadataFromTransactionRequest(invalidTxId, 21325);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("Metadata not found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Getting_metadata_with_invalid_key_returns_failure(int invalidKey)
    {
        // Arrange
        var request = new GetMetadataFromTransactionRequest(TestTxId, invalidKey);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("Metadata not found");
    }
}