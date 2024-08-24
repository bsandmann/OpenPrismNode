using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;

public partial class IntegrationTests
{
    private async Task<(string, byte[], int)> SetupExistingDid(LedgerType ledgerType, int epochNumber, int blockHeight, string did)
    {
        var blockHash = new byte[] { 1, 2, 3, 4 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var transactionHash = Hash.CreateFrom(new byte[] { 5, 6, 7, 8 });
        var operationHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var signingKeyId = "existingKey";

        var publicKeys = new List<PrismPublicKey>
        {
            new PrismPublicKey(PrismKeyUsage.MasterKey, signingKeyId, "secp256k1", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 })
        };
        var services = new List<PrismService>
        {
            new PrismService("existingService", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.com") })
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

        var result = await _createTransactionCreateDidHandler.Handle(createDidRequest, CancellationToken.None);

        if (result.IsFailed)
        {
            throw new Exception("Failed to set up existing DID: " + result.Errors.First().Message);
        }

        var createdDid = await _context.CreateDidEntities.FirstOrDefaultAsync(d => d.Did == PrismEncoding.HexToByteArray(did));
        if (createdDid == null)
        {
            throw new Exception("CreateDidEntity was not found in the database after creation.");
        }


        return (did, blockHash, blockHeight);
    }

    [Fact]
    public async Task CreateTransactionUpdateDid_Succeeds_For_Default_Case()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 4;
        var blockHeight = 4;
        var did = await SetupExistingDid(ledgerType, epochNumber, blockHeight, "843c02359b856b87027a57db385233338f3f13320377cf67a4744840ab164d77");

        // Set up for update
        blockHeight++;
        var newBlockHash = new byte[] { 5, 4, 7, 7 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, newBlockHash, did.Item2, did.Item3);

        var transactionHash = Hash.CreateFrom(new byte[] { 15, 24, 15, 17 });
        var operationHash = Hash.CreateFrom(new byte[] { 17, 28, 19, 27 });
        var signingKeyId = "existingKey";

        var existingDid = await _context.CreateDidEntities
            .Include(d => d.PrismPublicKeys)
            .FirstOrDefaultAsync(d => d.Did == PrismEncoding.HexToByteArray(did.Item1));

        var updateActions = new List<UpdateDidActionResult>
        {
            new UpdateDidActionResult(new PrismPublicKey(PrismKeyUsage.IssuingKey, "newKey", "secp256k1", new byte[] { 5, 7, 7 }, new byte[] { 8, 7, 10 })),
            new UpdateDidActionResult("existingService", true) // Remove existing service
        };
        
        var request = new CreateTransactionUpdateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(newBlockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            previousOperationHash: Hash.CreateFrom(existingDid.OperationHash),
            did: PrismEncoding.ByteArrayToHex(existingDid.Did),
            signingKeyId: signingKeyId,
            updateDidActions: updateActions,
            operationSequenceNumber: 2,
            utxos: new List<UtxoWrapper>()
        );

        // Act
        var result = await _createTransactionUpdateDidHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the transaction was created in the database
        var savedTransaction = await _context.TransactionEntities
            .FirstOrDefaultAsync(t => t.TransactionHash == transactionHash.Value);
        Assert.NotNull(savedTransaction);
        Assert.Equal(blockHeight, savedTransaction.BlockHeight);
        Assert.Equal(1000, savedTransaction.Fees);
        Assert.Equal(256, savedTransaction.Size);
        Assert.Equal(0, savedTransaction.Index);

        // Verify the UpdateDid operation was created
        var savedUpdateDid = await _context.UpdateDidEntities
            .Include(p => p.PrismServices)
            .Include(p => p.PrismPublicKeysToAdd)
            .Include(p => p.PrismPublicKeysToRemove)
            .Include(p => p.PatchedContexts)
            .FirstOrDefaultAsync(u => u.Did == PrismEncoding.HexToByteArray(did.Item1));
        Assert.NotNull(savedUpdateDid);
        Assert.Equal(operationHash.Value, savedUpdateDid.OperationHash);
        Assert.Equal(signingKeyId, savedUpdateDid.SigningKeyId);
        Assert.Equal(2, savedUpdateDid.OperationSequenceNumber);

        // Verify the new key was added
        Assert.Single(savedUpdateDid.PrismPublicKeysToAdd);
        var addedKey = savedUpdateDid.PrismPublicKeysToAdd.First();
        Assert.Equal("newKey", addedKey.KeyId);
        Assert.Equal(PrismKeyUsage.IssuingKey, addedKey.PrismKeyUsage);

        // Verify the service was removed
        Assert.Single(savedUpdateDid.PrismServices);
        var removedService = savedUpdateDid.PrismServices.First();
        Assert.Equal("existingService", removedService.ServiceId);
        Assert.True(removedService.Removed);
    }
}