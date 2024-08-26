using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateLedger;
using OpenPrismNode.Core.Commands.CreateEpoch;
using OpenPrismNode.Core.Commands.CreateBlock;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Entities;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Common;

public partial class IntegrationTests
{
    private async Task SetupLedgerEpochAndBlock(LedgerType ledgerType, int epochNumber, int blockHeight, byte[] blockHash, byte[]? previousBlockHash = null, int? previousBlockHeight = null)
    {
        // Create Ledger
        await _createLedgerHandler.Handle(new CreateLedgerRequest(ledgerType), CancellationToken.None);

        // Create Epoch
        await _createEpochHandler.Handle(new CreateEpochRequest(ledgerType, epochNumber), CancellationToken.None);

        // Create Block
        var createBlockRequest = new CreateBlockRequest(
            ledgerType: ledgerType,
            blockHash: Hash.CreateFrom(blockHash),
            previousBlockHash: previousBlockHash is null ? null : Hash.CreateFrom(previousBlockHash), // Assuming this is the first block
            blockHeight: blockHeight,
            previousBlockHeight: previousBlockHeight,
            epochNumber: epochNumber,
            timeUtc: DateTime.UtcNow,
            txCount: 0
        );
        await _createBlockHandler.Handle(createBlockRequest, CancellationToken.None);
    }

    [Fact]
    public async Task CreateTransactionCreateDid_Succeeds_For_Default_Case()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 3;
        var blockHeight = 3;
        var blockHash = new byte[] { 1, 6, 7, 8 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var did = "9c96389a50a41e1bb0dac7e786cc646c33f57f514ae96b6375e0b56ff505ecc2";
        var transactionHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var operationHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var signingKeyId = "key1";
        var publicKeys = new List<PrismPublicKey>
        {
            new PrismPublicKey(PrismKeyUsage.MasterKey, "key1", "secp256k1", new byte[] { 1, 2, 1 }, new byte[] { 4, 1, 6 })
        };
        var services = new List<PrismService>
        {
            new PrismService("service1", "LinkedDomains", new ServiceEndpoints { Uri = new Uri("https://example.com") })
        };

        var request = new CreateTransactionCreateDidRequest(
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
            prismServices: services,
            patchedContexts: new List<string>() { "https://first.com", "https://second.com" }
        );

        // Act
        var result = await _createTransactionCreateDidHandler.Handle(request, CancellationToken.None);

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

        // Verify the CreateDid operation was created
        var savedCreateDid = await _context.CreateDidEntities
            .Include(p => p.PrismPublicKeys)
            .Include(p => p.PrismServices)
            .Include(p => p.PatchedContext)
            .FirstOrDefaultAsync(c => c.OperationHash == PrismEncoding.HexToByteArray(did));
        Assert.NotNull(savedCreateDid);
        Assert.Equal(operationHash.Value, savedCreateDid.OperationHash);
        Assert.Equal(signingKeyId, savedCreateDid.SigningKeyId);
        Assert.Equal(1, savedCreateDid.OperationSequenceNumber);

        // Verify public keys were saved
        Assert.Single(savedCreateDid.PrismPublicKeys);
        var savedPublicKey = savedCreateDid.PrismPublicKeys.First();
        Assert.Equal("key1", savedPublicKey.KeyId);
        Assert.Equal(PrismKeyUsage.MasterKey, savedPublicKey.PrismKeyUsage);

        // Verify services were saved
        Assert.Single(savedCreateDid.PrismServices);
        var savedService = savedCreateDid.PrismServices.First();
        Assert.Equal("service1", savedService.ServiceId);
        Assert.Equal("LinkedDomains", savedService.Type);
        Assert.Equal("https://example.com/", savedService.Uri.ToString());

        // Verify contexts were saved
        Assert.NotNull(savedCreateDid.PatchedContext);
        Assert.True(savedCreateDid.PatchedContext.ContextList.Count == 2);
    }

    [Fact]
    public async Task CreateTransactionCreateDid_Succeeds_For_Existing_Transaction()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 1;
        var blockHeight = 1;
        var blockHash = new byte[] { 5, 6, 7, 8 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var did = "060e60bc35ce7f665c123fb1934c3a06d5cf6124f3a8f4cfd723b38762168d32";
        var transactionHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));
        var operationHash = Hash.CreateFrom(PrismEncoding.HexToByteArray(did));

        // Create an existing transaction
        _context.TransactionEntities.Add(new TransactionEntity
        {
            TransactionHash = transactionHash.Value,
            BlockHeight = blockHeight,
            BlockHashPrefix = BlockEntity.CalculateBlockHashPrefix(blockHash) ?? 0,
            Fees = 1000,
            Size = 256,
            Index = 0
        });
        await _context.SaveChangesAsync();

        var request = new CreateTransactionCreateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(blockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            did: did,
            signingKeyId: "key1",
            operationSequenceNumber: 1,
            utxos: new List<UtxoWrapper>(),
            prismPublicKeys: new List<PrismPublicKey>(),
            prismServices: new List<PrismService>(),
            patchedContexts: new List<string>()
        );

        // Act
        var result = await _createTransactionCreateDidHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        // The handler should succeed but not create a new transaction
        var transactionCount = await _context.TransactionEntities.CountAsync();
        Assert.Equal(1, transactionCount);

        // Verify that a CreateDid operation was added to the existing transaction
        var savedCreateDid = await _context.CreateDidEntities
            .Include(p => p.PatchedContext)
            .FirstOrDefaultAsync(c => c.OperationHash == PrismEncoding.HexToByteArray(did));
        Assert.NotNull(savedCreateDid);

        // Verify contexts were saved
        Assert.Null(savedCreateDid.PatchedContext);
    }
}