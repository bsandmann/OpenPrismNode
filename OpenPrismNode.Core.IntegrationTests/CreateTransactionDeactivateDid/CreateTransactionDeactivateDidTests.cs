using OpenPrismNode.Core.Commands.CreateTransactionCreateDid;
using OpenPrismNode.Core.Commands.CreateTransactionDeactivateDid;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Entities;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Common;

public partial class IntegrationTests
{
    [Fact]
    public async Task CreateTransactionDeactivateDid_Succeeds_For_Default_Case()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 5;
        var blockHeight = 6;
        var did = "9c96389a50a41e1bb0dac7e786cc646c33f57f514ae96b6375e0b56ff505ecca";
        await SetupExistingDid(ledgerType, epochNumber, blockHeight, did);

        // Set up for deactivation
        blockHeight++;
        var newBlockHash = new byte[] { 9, 2, 7, 8 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, newBlockHash);

        var transactionHash = Hash.CreateFrom(new byte[] { 11, 14, 15, 16 });
        var operationHash = Hash.CreateFrom(new byte[] { 11, 18, 19, 20 });
        var signingKeyId = "existingKey";

        var existingDid = await _context.CreateDidEntities
            .FirstOrDefaultAsync(d => d.OperationHash == PrismEncoding.HexToByteArray(did));

        var request = new CreateTransactionDeactivateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(newBlockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            previousOperationHash: Hash.CreateFrom(existingDid.OperationHash),
            did: did,
            signingKeyId: signingKeyId,
            operationSequenceNumber: 2,
            utxos: new List<UtxoWrapper>()
        );

        // Act
        var result = await _createTransactionDeactivateDidHandler.Handle(request, CancellationToken.None);

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

        // Verify the DeactivateDid operation was created
        var savedDeactivateDid = await _context.DeactivateDidEntities
            .FirstOrDefaultAsync(d => d.Did == PrismEncoding.HexToByteArray(did));
        Assert.NotNull(savedDeactivateDid);
        Assert.Equal(operationHash.Value, savedDeactivateDid.OperationHash);
        Assert.Equal(signingKeyId, savedDeactivateDid.SigningKeyId);
        Assert.Equal(2, savedDeactivateDid.OperationSequenceNumber);
    }

    [Fact]
    public async Task CreateTransactionDeactivateDid_Fails_For_Non_Existing_Did()
    {
        // Arrange
        var ledgerType = LedgerType.CardanoPreprod;
        var epochNumber = 3;
        var blockHeight = 8;
        var blockHash = new byte[] { 1, 2, 3, 4 };
        await SetupLedgerEpochAndBlock(ledgerType, epochNumber, blockHeight, blockHash);

        var did = "ac96389a50a41e1bb0dac7e786cc646c33f57f514ae96b6375e0b56ff505ecc2";
        var transactionHash = Hash.CreateFrom(new byte[] { 5, 6, 7, 8 });
        var operationHash = Hash.CreateFrom(new byte[] { 9, 10, 11, 12 });
        var previousOperationHash = Hash.CreateFrom(new byte[] { 13, 14, 15, 16 });
        var signingKeyId = "nonExistingKey";

        var request = new CreateTransactionDeactivateDidRequest(
            transactionHash: transactionHash,
            blockHash: Hash.CreateFrom(blockHash),
            blockHeight: blockHeight,
            fees: 1000,
            size: 256,
            index: 0,
            operationHash: operationHash,
            previousOperationHash: previousOperationHash,
            did: did,
            signingKeyId: signingKeyId,
            operationSequenceNumber: 1,
            utxos: new List<UtxoWrapper>()
        );

        // Act
        var result = await _createTransactionDeactivateDidHandler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Invalid operation ", result.Errors.First().Message);
    }
}