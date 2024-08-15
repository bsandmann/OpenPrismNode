namespace OpenPrismNode.Core.Commands.CreateTransactionCreateDid;

using System.Text.Json;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Entities;

/// <summary>
/// Handler a write a CreateDid-PRISM-Operation in the node database
/// </summary>
public class CreateTransactionCreateDidHandler : IRequestHandler<CreateTransactionCreateDidRequest, Result<TransactionModel?>>
{
    private readonly DataContext _context;
    private readonly ILogger<CreateTransactionCreateDidHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public CreateTransactionCreateDidHandler(DataContext context, ILogger<CreateTransactionCreateDidHandler> logger)
    {
        this._context = context;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TransactionModel?>> Handle(CreateTransactionCreateDidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            var hasExistingTransaction = await _context.TransactionEntities
                .AnyAsync(t => t.TransactionHash == request.TransactionHash.Value, cancellationToken);
            if (!hasExistingTransaction)
            {
                var existingDid = await _context.CreateDidEntities.AnyAsync(p => p.Did == PrismEncoding.HexToByteArray(request.Did), cancellationToken: cancellationToken);
                if (existingDid)
                {
                    _logger.LogWarning($"CreateDid-Operation for {request.Did} already exists in the database. This is a problem with the integrety of the ledger. TransactionHash: {request.TransactionHash.Value} Block: {request.BlockHeight}");
                    return Result.Ok();
                }
                
                var prefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value);
                var trans = new TransactionEntity()
                {
                    TransactionHash = request.TransactionHash.Value,
                    Fees = request.Fees,
                    Size = request.Size,
                    Index = request.Index,
                    BlockHeight = request.BlockHeight,
                    BlockHashPrefix = prefix!.Value,
                    Utxos = request.Utxos.DistinctBy(p => (p.Value, p.IsOutgoing, p.Index)).Select(p => new UtxoEntity()
                    {
                        Index = p.Index,
                        Value = p.Value,
                        IsOutgoing = p.IsOutgoing,
                        StakeAddress = p.WalletAddress.StakeAddressString,
                        WalletAddress = p.WalletAddress.WalletAddressString
                    }).ToList(),
                    CreateDidEntities = new List<CreateDidEntity>()
                    {
                        new CreateDidEntity()
                        {
                            OperationHash = request.OperationHash.Value!,
                            OperationSequenceNumber = request.OperationSequenceNumber,
                            Did = PrismEncoding.HexToByteArray(request.Did),
                            SigningKeyId = request.SigningKeyId,
                            TransactionHash = request.TransactionHash.Value,
                            BlockHeight = request.BlockHeight,
                            BlockHashPrefix = prefix!.Value,
                            PrismPublicKeys = request.PrismPublicKeys.Select(p => new PrismPublicKeyEntity()
                            {
                                KeyId = p.KeyId,
                                PublicKey = p.LongByteArray,
                                PrismKeyUsage = p.KeyUsage,
                                Curve = p.Curve
                            }).ToList(),
                            PrismServices = request.PrismServices.Select(p => new PrismServiceEntity()
                            {
                                ServiceId = p.ServiceId,
                                Type = p.Type,
                                Uri = p.ServiceEndpoints.Uri,
                                ListOfUris = p.ServiceEndpoints.ListOfUris,
                                JsonData = p.ServiceEndpoints.Json is not null ? JsonSerializer.Serialize(p.ServiceEndpoints.Json) : null
                            }).ToList()
                        }
                    }
                };
                await _context.TransactionEntities.AddAsync(trans, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // var prismCreateDidEntity =
                //     new PrismCreateDidEntity()
                //     {
                //         TransactionHash = request.TransactionHash.Value,
                //         OperationHash = request.OperationHash.Value,
                //         OperationSequenceNumber = request.OperationSequenceNumber,
                //         Did = PrismEncoding.HexToByteArray(request.Did),
                //         SigningKeyId = request.SigningKeyId,
                //         PrismPublicKeys = request.PublicKeyModels.Select(p => new PrismPublicKeyEntity()
                //         {
                //             KeyId = p.KeyId,
                //             PublicKey = PrismEncoding.HexToByteArray(p.PublicKey),
                //             PrismKeyUsage = p.KeyUsage,
                //             Curve = p.Curve
                //         }).ToList(),
                //         PrismServices = request.ServiceModels.Select(p => new PrismServiceEntity()
                //         {
                //             ServiceId = p.ServiceId,
                //             Type = p.Type,
                //             ServiceEndpoints = p.SerivceEndpoints,
                //             Removed = false,
                //             Updated = false,
                //         }).ToList()
                //     };
                //
                // await _context.PrismCreateDidEntities.AddAsync(prismCreateDidEntity, cancellationToken);
                // await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok(new TransactionModel(
                transactionHash: request.TransactionHash,
                blockHash: request.BlockHash,
                fees: request.Fees,
                size: request.Size,
                index: request.Index
                // createDidOperations: new List<PrismCreateDidModel>()
                // {
                //     new PrismCreateDidModel(
                //         operationHash: request.OperationHash,
                //         operationSequenceNumber: request.OperationSequenceNumber,
                //         did: request.Did,
                //         signingKeyId: request.SigningKeyId,
                //         publicKeys: request.PublicKeyModels,
                //         services: request.ServiceModels),
                // },
                // updateDidOperations: new List<PrismUpdateDidModel>(),
                // deactivateDidOperations: new List<PrismDeactivateDidModel>(),
                // issueCredentialBatchOperations: new List<PrismIssueCredentialBatchModel>(),
                // revokeCredentialsOperations: new List<PrismRevokeCredentialsModel>(),
                // protocolVersionUpdateOperations: new List<PrismProtocolVersionUpdateModel>(),
                // incomingUtxos: request.IncomingUtxos,
                // outgoingUtxos: request.OutgoingUtxos
            ));
        }
        // catch ( UniqueConstraintException)
        // {
        //     return Result.Fail($"Invalid CreateDid-Operation: Unique constraint for DID: {request.Did}");
        // }
        catch (Exception e)
        {
            // return Result.Fail($"Invalid operation when saving a CreateDid-Operation: blockHash : '{request.PrismBlockHash.AsHex()}'. Message: {e.Message} Inner: {e.InnerException?.Message}");
            return Result.Fail("");
        }
    }
}