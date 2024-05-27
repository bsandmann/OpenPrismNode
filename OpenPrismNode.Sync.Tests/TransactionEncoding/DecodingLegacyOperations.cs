namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using Commands.DecodeTransaction;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Models;
using TestDocuments;

public class DecodingLegacyOperations
{
    [Fact]
    public async Task IssueCredential_Transaction_decodes_correctly_for_Prism_v1()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV1_IssueCredential_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.None);
    }
}