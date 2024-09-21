namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using System.Text.Json;
using System.Text.Json.Nodes;
using Commands.DecodeTransaction;
using Core.Commands.EncodeTransaction;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using TestDocuments;

public class EncodingCreateDid
{
    // NOTE: Roundtrip operations fail for PRISM v1, due to the removal of the optional BlockByteLength and BlockOperation Count
    // vales in the AtalaObject. These values are marked as "reserved" which prevents them from being serialized to.

    // The roundtrip also fails for PRISM v2 transactions, which happend before ~Winter 2023, since the reserved-fields have
    // only been effect around this time.

    [Fact]
    public async Task CreateDid_roundtrip_encoding_for_Prism_v2()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var decodeHandler = new DecodeTransactionHandler();
        var decodedResult = await decodeHandler.Handle(decodeTransactionRequest, new CancellationToken());
        var encodeHandler = new EncodeTransactionHandler();

        // Act
        var roundTripResult = await encodeHandler.Handle(new EncodeTransactionRequest(decodedResult.Value), new CancellationToken());

        // Assert
        roundTripResult.Should().BeSuccess();
        JsonNode? originalTransaction = JsonNode.Parse(serializedTransaction);
        JsonNode? roundTripTransaction = JsonNode.Parse(JsonSerializer.Serialize(roundTripResult.Value));
        JsonNode.DeepEquals(originalTransaction, roundTripTransaction).Should().BeTrue();
    }

    [Fact]
    public async Task CreateDid_roundtrip_encoding_for_Prism_v2_with_services()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction_with_services;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var decodeHandler = new DecodeTransactionHandler();
        var decodedResult = await decodeHandler.Handle(decodeTransactionRequest, new CancellationToken());
        var encodeHandler = new EncodeTransactionHandler();

        // Act
        var roundTripResult = await encodeHandler.Handle(new EncodeTransactionRequest(decodedResult.Value), new CancellationToken());

        // Assert
        roundTripResult.Should().BeSuccess();
        JsonNode? originalTransaction = JsonNode.Parse(serializedTransaction);
        JsonNode? roundTripTransaction = JsonNode.Parse(JsonSerializer.Serialize(roundTripResult.Value));
        JsonNode.DeepEquals(originalTransaction, roundTripTransaction).Should().BeTrue();
    }
}