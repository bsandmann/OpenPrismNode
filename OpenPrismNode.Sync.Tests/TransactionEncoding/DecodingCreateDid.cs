namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using Commands.DecodeTransaction;
using Core;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Models;
using TestDocuments;

public class DecodingCreateDid
{
    [Fact]
    public async Task CreateDid_Transaction_decodes_correctly_for_Prism_v1()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV1_CreateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.CreateDid);
        result.Value.Single().Operation.CreateDid.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Context.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Context.Count.Should().Be(0);
        result.Value.Single().Operation.CreateDid.DidData.Services.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Services.Count.Should().Be(0);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys.Count.Should().Be(3);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].Id.Should().Be("master0");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].Usage.Should().Be(KeyUsage.MasterKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].Id.Should().Be("issuing0");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].Usage.Should().Be(KeyUsage.IssuingKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].Id.Should().Be("revocation0");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].Usage.Should().Be(KeyUsage.RevocationKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Signature.Should().NotBeNull();
        result.Value.Single().SignedWith.Should().Be("master0");
    }

    [Fact]
    public async Task CreateDid_Transaction_decodes_correctly_for_Prism_v2()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.CreateDid);
        result.Value.Single().Operation.CreateDid.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Context.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Context.Count.Should().Be(0);
        result.Value.Single().Operation.CreateDid.DidData.Services.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Services.Count.Should().Be(0);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys.Count.Should().Be(3);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].Id.Should().Be("key1");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].Usage.Should().Be(KeyUsage.AuthenticationKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].Id.Should().Be("key2");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].Usage.Should().Be(KeyUsage.IssuingKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].Id.Should().Be("master0");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].Usage.Should().Be(KeyUsage.MasterKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Signature.Should().NotBeNull();
        result.Value.Single().SignedWith.Should().Be("master0");
    }

    [Fact]
    public async Task CreateDid_Transaction_decodes_correctly_for_Prism_v2_with_multiple_operations()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV1_CreateDid_Transaction_with_two_operations;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(2);
        result.Value[0].Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.CreateDid);
        result.Value[0].Operation.CreateDid.Should().NotBeNull();
        result.Value[0].Operation.CreateDid.DidData.Should().NotBeNull();
        result.Value[0].Operation.CreateDid.DidData.Context.Should().NotBeNull();
        result.Value[0].Operation.CreateDid.DidData.Context.Count.Should().Be(0);
        result.Value[0].Operation.CreateDid.DidData.Services.Should().NotBeNull();
        result.Value[0].Operation.CreateDid.DidData.Services.Count.Should().Be(0);
        result.Value[0].Operation.CreateDid.DidData.PublicKeys.Should().NotBeNull();
        result.Value[0].Operation.CreateDid.DidData.PublicKeys.Count.Should().Be(1);
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].Id.Should().Be("master0");
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].Usage.Should().Be(KeyUsage.MasterKey);
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].EcKeyData.Should().BeNull();
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Should().NotBeNull();
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value[0].Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value[0].Signature.Should().NotBeNull();
        result.Value[0].SignedWith.Should().Be("master0");
        result.Value[1].Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.CreateDid);
        result.Value[1].Operation.CreateDid.Should().NotBeNull();
        result.Value[1].Operation.CreateDid.DidData.Should().NotBeNull();
        result.Value[1].Operation.CreateDid.DidData.Context.Should().NotBeNull();
        result.Value[1].Operation.CreateDid.DidData.Context.Count.Should().Be(0);
        result.Value[1].Operation.CreateDid.DidData.Services.Should().NotBeNull();
        result.Value[1].Operation.CreateDid.DidData.Services.Count.Should().Be(0);
        result.Value[1].Operation.CreateDid.DidData.PublicKeys.Should().NotBeNull();
        result.Value[1].Operation.CreateDid.DidData.PublicKeys.Count.Should().Be(1);
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].Id.Should().Be("master0");
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].Usage.Should().Be(KeyUsage.MasterKey);
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].EcKeyData.Should().BeNull();
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Should().NotBeNull();
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value[1].Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value[1].Signature.Should().NotBeNull();
        result.Value[1].SignedWith.Should().Be("master0");
    }
    
     [Fact]
    public async Task CreateDid_Transaction_decodes_correctly_for_Prism_v2_with_services()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction_with_services;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.CreateDid);
        result.Value.Single().Operation.CreateDid.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Context.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Context.Count.Should().Be(0);
        result.Value.Single().Operation.CreateDid.DidData.Services.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.Services.Count.Should().Be(1);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys.Count.Should().Be(3);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].Id.Should().Be("key-1");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].Usage.Should().Be(KeyUsage.AuthenticationKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[0].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].Id.Should().Be("key-2");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].Usage.Should().Be(KeyUsage.IssuingKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[1].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].Id.Should().Be("master0");
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].Usage.Should().Be(KeyUsage.MasterKey);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].EcKeyData.Should().BeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Should().NotBeNull();
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.CreateDid.DidData.PublicKeys[2].CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.CreateDid.DidData.Services.Count.Should().Be(1);
        result.Value.Single().Operation.CreateDid.DidData.Services[0].Id.Should().Be("service-1");
        result.Value.Single().Operation.CreateDid.DidData.Services[0].Type.Should().Be("LinkedDomains");
        result.Value.Single().Operation.CreateDid.DidData.Services[0].ServiceEndpoint.Should().Be("[\"https://m4.csign.io/\"]");
        result.Value.Single().Signature.Should().NotBeNull();
        result.Value.Single().SignedWith.Should().Be("master0");
    }
}