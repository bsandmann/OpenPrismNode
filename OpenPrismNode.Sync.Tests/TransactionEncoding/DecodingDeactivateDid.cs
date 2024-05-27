namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using Commands.DecodeTransaction;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Models;
using TestDocuments;

public class DecodingDeactivateDid
{
    [Fact]
    public async Task DeactivateDid_Transaction_decodes_correctly_for_Prism_v2()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_DeactivateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.DeactivateDid);
        result.Value.Single().Operation.DeactivateDid.Should().NotBeNull();
        result.Value.Single().Operation.DeactivateDid.Id.Should().Be("7af5a9f0c36ace08f5885aa069721003cde040b39c7f3c20d8fa4d87273d38cd");
        result.Value.Single().Operation.DeactivateDid.PreviousOperationHash.Length.Should().Be(32);
    }
}