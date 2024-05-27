namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using Commands.DecodeTransaction;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Models;
using TestDocuments;

public class DecodingErros
{
    [Fact]
    public async Task CreateDid_Transaction_fails_for_invalid_json()
    {
        // Arrange
        var invalidJson = """
                          {
                              "c": {
                                  "0x10012299021296020a076d6173746572301246304402203a9c75d64142ac60f0c0235a6b1dce7a15c9106dbc57f8889148c6992807531c02202130c2d656abb0",
                                  "0x74272535202c1fa5fabb12a6de5960b2ae724c000efcf6a2581ac2010abf010abc01123b0a076d61737465723010014a2e0a09736563703235366b31122102a5",
                                  "0xaca82ee1bf35ccf3ca1e47f2e7f6a6fed37836eedeed6ca81f767703c7a212123c0a0869737375696e673010024a2e0a09736563703235366b31122102923435",
                                  "0x2f6306e998ae33b8d9d556fd1e799775b4bb882ba4f52acd6fd363bc3c123f0a0b7265766f636174696f6e3010054a2e0a09736563703235366b311221037c9e",
                                  "0x06a1d119c8256c673d808f0cf3f9cb17f2238158ac5cef0b30058d485f0e"
                              },
                              "v": 1
                          }
                          """;
        var serializedTransaction = invalidJson;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeFailure();
        result.Errors.Single().Message.Should().Be(ParserErrors.UnableToDeserializeBlockchainMetadata);
    }

    [Fact]
    public async Task CreateDid_Transaction_fails_for_invalid_blockcontent()
    {
        // Arrange
        var invalidJson = """
                          {
                              "c": [
                                  "0x10012299021296020a076d6173746572301246304402203a9c75d64142ac60f0c0235a6b1dce7a15c9106dbc57f8889148c6992807531c02202130c2d656abb0"
                              ],
                              "v": 1
                          }
                          """;
        var serializedTransaction = invalidJson;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeFailure();
        result.Errors.Single().Message.Should().Be(ParserErrors.ParsingFailedInvalidForm);
    }
    
    [Fact]
    public async Task CreateDid_Transaction_fails_for_unsupported_version()
    {
        // Arrange
        var invalidJson =  """
                           {
                               "c": [
                                   "0x228f02128c020a076d61737465723012473045022100ae7ce823688ecf655b0869f4cecbb974f3edf390cbba7388a71122ddbe6cba7e02205b6c9a604ab2fccd",
                                   "0x3145594249e90434847b45824b081c7162f3365b4c0066ad1ab7010ab4010ab10112380a046b65793110044a2e0a09736563703235366b31122102f84061c89d",
                                   "0xd21df4706244cae7f40ec97dd7ffd7d783d8eb32ebaba7d14e907a12380a046b65793210024a2e0a09736563703235366b31122102b1658ba508309a371ec333",
                                   "0xfedfa034894979734ce7ea47274b02804168258f6f123b0a076d61737465723010014a2e0a09736563703235366b31122102e894412721c7a42a01a20f1a6edf",
                                   "0x58b3f8c6ee3d221512b1742012eaae1f7d4f"
                               ],
                               "v": 42
                           }
                           """;;
        var serializedTransaction = invalidJson;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeFailure();
        result.Errors.Single().Message.Should().Be(ParserErrors.UnsupportedPrismBlockVersion);
    }
}