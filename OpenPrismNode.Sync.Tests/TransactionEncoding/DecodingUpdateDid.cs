namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using Commands.DecodeTransaction;
using Core;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Models;
using TestDocuments;

public class DecodingUpdateDid
{
    [Fact]
    public async Task UpdateDid_Transaction_decodes_correctly_for_Prism_v2()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_Legacy_UpdateDid_Transaction_with_4Actions;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.UpdateDid);
        result.Value.Single().Operation.UpdateDid.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Id.Should().Be("e212fbe6d0a9be9021fb77927364898934a0faccd67bcdf70ec84271825848e1");
        result.Value.Single().Operation.UpdateDid.PreviousOperationHash.Length.Should().Be(32);
        result.Value.Single().Operation.UpdateDid.Actions.Count.Should().Be(4);
        result.Value.Single().Operation.UpdateDid.Actions[0].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.AddKey);
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.Id.Should().Be("key3");
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.EcKeyData.Should().BeNull();
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value.Single().Operation.UpdateDid.Actions[1].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.RemoveKey);
        result.Value.Single().Operation.UpdateDid.Actions[1].RemoveKey.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Actions[1].RemoveKey.KeyId.Should().Be("key1");
        result.Value.Single().Operation.UpdateDid.Actions[2].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.RemoveService);
        result.Value.Single().Operation.UpdateDid.Actions[2].RemoveService.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Actions[2].RemoveService.ServiceId.Should().Be("did:prism:test1");
        result.Value.Single().Operation.UpdateDid.Actions[3].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.AddService);
        result.Value.Single().Operation.UpdateDid.Actions[3].AddService.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Actions[3].AddService.Service.Id.Should().Be("did:prism:test3added");
        result.Value.Single().Operation.UpdateDid.Actions[3].AddService.Service.ServiceEndpoint.Should().Be("https://bar.example.com/");
        result.Value.Single().Operation.UpdateDid.Actions[3].AddService.Service.Type.Should().Be("LinkedDomains");
    }
    
    [Fact]
    public async Task UpdateDid_Transaction_decodes_correctly_for_Prism_v1()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV1_UpdateDid_Transaction;
        var decodeTransactionRequest = new DecodeTransactionRequest(serializedTransaction);
        var handler = new DecodeTransactionHandler();

        // Act
        var result = await handler.Handle(decodeTransactionRequest, new CancellationToken());

        // Assert
        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value.Single().Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.UpdateDid);
        result.Value.Single().Operation.UpdateDid.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Id.Should().Be("8f015e605e2076bd58570ab2bd68ae8f0f03278db41cec8ca042dc077f797977");
        result.Value.Single().Operation.UpdateDid.PreviousOperationHash.Length.Should().Be(32);
        result.Value.Single().Operation.UpdateDid.Actions.Count.Should().Be(1);
        result.Value.Single().Operation.UpdateDid.Actions[0].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.AddKey);
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Should().NotBeNull();
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.Id.Should().Be("issuing0");
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.EcKeyData.Should().BeNull();
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value.Single().Operation.UpdateDid.Actions[0].AddKey.Key.CompressedEcKeyData.Data.Length.Should().Be(33);
    }
    
    [Fact]
    public async Task UpdateDid_Transaction_decodes_correctly_for_Prism_v1_with_two_operations()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV1_UpdateDid_Transaction_with_two_operations;
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
        result.Value[1].Operation.OperationCase.Should().Be(AtalaOperation.OperationOneofCase.UpdateDid);
        result.Value[1].Operation.UpdateDid.Should().NotBeNull();
        result.Value[1].Operation.UpdateDid.Id.Should().Be("27e010f0bf7e03863fce4bcb9dca9ba246b6c1033e82d6168e9573449496350f");
        result.Value[1].Operation.UpdateDid.PreviousOperationHash.Length.Should().Be(32);
        result.Value[1].Operation.UpdateDid.Actions.Count.Should().Be(2);
        result.Value[1].Operation.UpdateDid.Actions[0].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.AddKey);
        result.Value[1].Operation.UpdateDid.Actions[0].AddKey.Should().NotBeNull();
        result.Value[1].Operation.UpdateDid.Actions[0].AddKey.Key.Id.Should().Be("issuing0");
        result.Value[1].Operation.UpdateDid.Actions[0].AddKey.Key.KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value[1].Operation.UpdateDid.Actions[0].AddKey.Key.EcKeyData.Should().BeNull();
        result.Value[1].Operation.UpdateDid.Actions[0].AddKey.Key.CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value[1].Operation.UpdateDid.Actions[0].AddKey.Key.CompressedEcKeyData.Data.Length.Should().Be(33);
        result.Value[1].Operation.UpdateDid.Actions[1].ActionCase.Should().Be(UpdateDIDAction.ActionOneofCase.AddKey);
        result.Value[1].Operation.UpdateDid.Actions[1].AddKey.Should().NotBeNull();
        result.Value[1].Operation.UpdateDid.Actions[1].AddKey.Key.Id.Should().Be("revocation0");
        result.Value[1].Operation.UpdateDid.Actions[1].AddKey.Key.KeyDataCase.Should().Be(PublicKey.KeyDataOneofCase.CompressedEcKeyData);
        result.Value[1].Operation.UpdateDid.Actions[1].AddKey.Key.EcKeyData.Should().BeNull();
        result.Value[1].Operation.UpdateDid.Actions[1].AddKey.Key.CompressedEcKeyData.Curve.Should().Be(PrismParameters.Secp256k1CurveName);
        result.Value[1].Operation.UpdateDid.Actions[1].AddKey.Key.CompressedEcKeyData.Data.Length.Should().Be(33);
    }
}