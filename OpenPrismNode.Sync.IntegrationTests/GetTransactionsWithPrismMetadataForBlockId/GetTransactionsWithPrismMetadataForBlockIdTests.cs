namespace OpenPrismNode.Sync.IntegrationTests.GetTransactionsWithPrismMetadataForBlockId;

using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using OpenPrismNode.Sync.Commands.GetTransactionsWithPrismMetadataForBlockId;
using OpenPrismNode.Core.Models;

public class GetTransactionsWithPrismMetadataForBlockIdTests
{
    private readonly TestNpgsqlConnectionFactory _testFactory;
    private readonly GetTransactionsWithPrismMetadataForBlockIdHandler _handler;
    private const int TestBlockId = 190363; // ! This is the block if of a block with PRISM metadata. THis number might be different with a new databas!
    

    public GetTransactionsWithPrismMetadataForBlockIdTests()
    {
        _testFactory = new TestNpgsqlConnectionFactory(TestSetup.PostgreSqlConnectionString);
        _handler = new GetTransactionsWithPrismMetadataForBlockIdHandler(_testFactory);
    }

    [Fact]
    public async Task Getting_transactions_with_prism_metadata_for_specific_block_succeeds()
    {
        // Arrange
        var request = new GetTransactionsWithPrismMetadataForBlockIdRequest(TestBlockId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().AllSatisfy(transaction =>
        {
            transaction.id.Should().BePositive();
            transaction.hash.Should().NotBeNullOrEmpty();
            transaction.block_index.Should().BeGreaterOrEqualTo(0);
            transaction.fee.Should().BeGreaterOrEqualTo(0);
            transaction.size.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task Getting_transactions_for_block_without_prism_metadata_returns_empty_list()
    {
        // Arrange
        var blockIdWithoutMetadata = 123; // Assume this block has no PRISM metadata
        var request = new GetTransactionsWithPrismMetadataForBlockIdRequest(blockIdWithoutMetadata);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Getting_transactions_for_nonexistent_block_returns_empty_list()
    {
        // Arrange
        var nonexistentBlockId = int.MaxValue; // Assume this block doesn't exist
        var request = new GetTransactionsWithPrismMetadataForBlockIdRequest(nonexistentBlockId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Getting_transactions_returns_correct_order()
    {
        // Arrange
        var request = new GetTransactionsWithPrismMetadataForBlockIdRequest(TestBlockId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().BeInAscendingOrder(t => t.block_index);
    }
}