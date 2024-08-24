using FluentAssertions;
using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateTransactionDeactivateDid;
using OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;
using OpenPrismNode.Core.Commands.ResolveDid;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;

public partial class IntegrationTests
{
    [Fact]
    public async Task ResolveDid_Succeeds_For_Newly_Created_Did()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 1;
        var blockHash = new byte[] { 7, 2, 8, 4 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var did = "ab3c02359b856b87027a57db385233338f3f13320377cf67a4744840ab164dba";
        var transactionHash = Hash.CreateFrom(new byte[] { 1, 8, 7, 8 });
        var operationHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var signingKeyId = "key1";

        var publicKeys = new List<PrismPublicKey>
        {
            new PrismPublicKey(PrismKeyUsage.MasterKey, signingKeyId, "secp256k1", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 })
        };
        var services = new List<PrismService>
        {
            new PrismService("service1", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.com") })
        };

        var createDidRequest = new CreateTransactionCreateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(blockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            did: did,
            signingKeyId: signingKeyId,
            operationSequenceNumber: 1,
            utxos: new List<UtxoWrapper>(),
            prismPublicKeys: publicKeys,
            prismServices: services
        );

        await _createTransactionCreateDidHandler.Handle(createDidRequest, CancellationToken.None);

        var resolveDidRequest = new ResolveDidRequest(did);

        // Act
        var result = await _resolveDidHandler.Handle(resolveDidRequest, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var resolvedDidDocument = result.Value.DidDocument;
        resolvedDidDocument.Should().NotBeNull();
        resolvedDidDocument.DidIdentifier.Should().Be(did);

        // Verify public keys
        resolvedDidDocument.PublicKeys.Should().HaveCount(1);
        var resolvedPublicKey = resolvedDidDocument.PublicKeys[0];
        resolvedPublicKey.KeyId.Should().Be(signingKeyId);
        resolvedPublicKey.KeyUsage.Should().Be(PrismKeyUsage.MasterKey);
        resolvedPublicKey.Curve.Should().Be("secp256k1");

        // Verify services
        resolvedDidDocument.PrismServices.Should().HaveCount(1);
        var resolvedService = resolvedDidDocument.PrismServices[0];
        resolvedService.ServiceId.Should().Be("service1");
        resolvedService.Type.Should().Be("LinkedDomains");
        resolvedService.ServiceEndpoints.Uri.Should().Be(new Uri("https://example.com"));
        
        // Verify contexts
        resolvedDidDocument.Contexts.Should().HaveCount(1); // Default context
    }

    [Fact]
    public async Task ResolveDid_Succeeds_For_Updated_Did()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 1;
        var blockHash = new byte[] { 7, 4, 8, 4 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var did = "ac3c02359b856b87027a57db385233338f3f13320377cf67a4744840ab164dba";
        var transactionHash = Hash.CreateFrom(new byte[] { 1, 9, 7, 8 });
        var operationHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var signingKeyId = "key1";

        var publicKeys = new List<PrismPublicKey>
        {
            new PrismPublicKey(PrismKeyUsage.MasterKey, signingKeyId, "secp256k1", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 })
        };
        var services = new List<PrismService>
        {
            new PrismService("service1", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.com") })
        };

        var createDidRequest = new CreateTransactionCreateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(blockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            did: did,
            signingKeyId: signingKeyId,
            operationSequenceNumber: 1,
            utxos: new List<UtxoWrapper>(),
            prismPublicKeys: publicKeys,
            prismServices: services
        );

        await _createTransactionCreateDidHandler.Handle(createDidRequest, CancellationToken.None);

        // Update the DID
        blockHeight++;
        var updateBlockHash = new byte[] { 8, 3, 9, 5 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, updateBlockHash);

        var updateTransactionHash = Hash.CreateFrom(new byte[] { 2, 9, 8, 9 });
        var updateOperationHash = Hash.CreateFrom(new byte[] { 3, 10, 9, 10 });
        var newSigningKeyId = "key2";

        var updateActions = new List<UpdateDidActionResult>
        {
            new UpdateDidActionResult(new PrismPublicKey(PrismKeyUsage.IssuingKey, newSigningKeyId, "secp256k1", new byte[] { 7, 8, 9 }, new byte[] { 10, 11, 12 })),
            new UpdateDidActionResult("service1", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.org") }),
            new UpdateDidActionResult(new List<string>() { "some new context", "some other new context" })

        };

        var updateDidRequest = new CreateTransactionUpdateDidRequest(
            transactionHash: updateTransactionHash,
            blockHash: Hash.CreateFrom(updateBlockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: updateOperationHash,
            previousOperationHash: operationHash,
            did: did,
            signingKeyId: signingKeyId,
            updateDidActions: updateActions,
            operationSequenceNumber: 2,
            utxos: new List<UtxoWrapper>()
        );

        await _createTransactionUpdateDidHandler.Handle(updateDidRequest, CancellationToken.None);

        var resolveDidRequest = new ResolveDidRequest(did);

        // Act
        var result = await _resolveDidHandler.Handle(resolveDidRequest, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var resolvedDidDocument = result.Value.DidDocument;
        resolvedDidDocument.Should().NotBeNull();
        resolvedDidDocument.DidIdentifier.Should().Be(did);

        // Verify public keys
        resolvedDidDocument.PublicKeys.Should().HaveCount(2);
        resolvedDidDocument.PublicKeys.Should().Contain(pk => pk.KeyId == signingKeyId && pk.KeyUsage == PrismKeyUsage.MasterKey);
        resolvedDidDocument.PublicKeys.Should().Contain(pk => pk.KeyId == newSigningKeyId && pk.KeyUsage == PrismKeyUsage.IssuingKey);

        // Verify services
        resolvedDidDocument.PrismServices.Should().HaveCount(1);
        resolvedDidDocument.PrismServices.Should().Contain(s => s.ServiceId == "service1" && s.Type == "LinkedDomains" && s.ServiceEndpoints.Uri == new Uri("https://example.org"));
       
        // Verify contexts
        resolvedDidDocument.Contexts.Should().HaveCount(3); // 2 from the update action and 1 default context
    }

    [Fact]
    public async Task ResolveDid_Succeeds_For_Updated_And_Deactivated_Did()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 1;
        var blockHash = new byte[] { 7, 2, 8, 2 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var did = "cc3c02359b856b87027a57db385233338f3f13320377cf67a4744840ab164dba";
        var transactionHash = Hash.CreateFrom(new byte[] { 1, 1, 9, 8 });
        var operationHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var signingKeyId = "key1";

        var publicKeys = new List<PrismPublicKey>
        {
            new PrismPublicKey(PrismKeyUsage.MasterKey, signingKeyId, "secp256k1", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 })
        };
        var services = new List<PrismService>
        {
            new PrismService("service1", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.com") })
        };

        var createDidRequest = new CreateTransactionCreateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(blockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            did: did,
            signingKeyId: signingKeyId,
            operationSequenceNumber: 1,
            utxos: new List<UtxoWrapper>(),
            prismPublicKeys: publicKeys,
            prismServices: services
        );

        await _createTransactionCreateDidHandler.Handle(createDidRequest, CancellationToken.None);

        // Update the DID
        blockHeight++;
        var updateBlockHash = new byte[] { 8, 3, 9, 5 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, updateBlockHash);

        var updateTransactionHash = Hash.CreateFrom(new byte[] { 2, 9, 8, 9 });
        var updateOperationHash = Hash.CreateFrom(new byte[] { 3, 10, 9, 10 });
        var newSigningKeyId = "key2";

        var updateActions = new List<UpdateDidActionResult>
        {
            new UpdateDidActionResult(new PrismPublicKey(PrismKeyUsage.IssuingKey, newSigningKeyId, "secp256k1", new byte[] { 7, 8, 9 }, new byte[] { 10, 11, 12 })),
            new UpdateDidActionResult("service1", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.org") }),
            new UpdateDidActionResult(new List<string>() { "some new context", "some other new context" })
        };

        var updateDidRequest = new CreateTransactionUpdateDidRequest(
            transactionHash: updateTransactionHash,
            blockHash: Hash.CreateFrom(updateBlockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: updateOperationHash,
            previousOperationHash: operationHash,
            did: did,
            signingKeyId: signingKeyId,
            updateDidActions: updateActions,
            operationSequenceNumber: 2,
            utxos: new List<UtxoWrapper>()
        );

        await _createTransactionUpdateDidHandler.Handle(updateDidRequest, CancellationToken.None);

        // Deactivate the DID
        blockHeight++;
        var deactivateBlockHash = new byte[] { 9, 4, 10, 6 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, deactivateBlockHash);

        var deactivateTransactionHash = Hash.CreateFrom(new byte[] { 3, 10, 9, 10 });
        var deactivateOperationHash = Hash.CreateFrom(new byte[] { 4, 11, 10, 11 });

        var deactivateDidRequest = new CreateTransactionDeactivateDidRequest(
            transactionHash: deactivateTransactionHash,
            blockHash: Hash.CreateFrom(deactivateBlockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: deactivateOperationHash,
            previousOperationHash: updateOperationHash,
            did: did,
            signingKeyId: signingKeyId,
            operationSequenceNumber: 3,
            utxos: new List<UtxoWrapper>()
        );

        await _createTransactionDeactivateDidHandler.Handle(deactivateDidRequest, CancellationToken.None);

        var resolveDidRequest = new ResolveDidRequest(did);

        // Act
        var result = await _resolveDidHandler.Handle(resolveDidRequest, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var resolvedDidDocument = result.Value.DidDocument;
        resolvedDidDocument.Should().NotBeNull();
        resolvedDidDocument.DidIdentifier.Should().Be(did);

        // Verify that no public keys or services are present in a deactivated DID
        resolvedDidDocument.PublicKeys.Should().BeEmpty();
        resolvedDidDocument.PrismServices.Should().BeEmpty();
        resolvedDidDocument.Contexts.Should().BeEmpty();
    }
}