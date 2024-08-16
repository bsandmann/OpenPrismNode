namespace OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;

using System.Text.Json;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

/// <summary>
/// Handler to write a UpdateDid-PRISM-Operation in the node database
/// </summary>
public class CreateTransactionUpdateDidHandler : IRequestHandler<CreateTransactionUpdateDidRequest, Result<TransactionModel>>
{
    private readonly DataContext _context;
    private readonly ILogger<CreateTransactionUpdateDidHandler> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    public CreateTransactionUpdateDidHandler(DataContext context, ILogger<CreateTransactionUpdateDidHandler> logger)
    {
        this._context = context;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TransactionModel>> Handle(CreateTransactionUpdateDidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            var operationOrderIndex = 0;
            var prismPublicKeysToAdd = new List<PrismPublicKeyEntity>();
            var prismPublicKeysToRemove = new List<PrismPublicKeyRemoveEntity>();
            var prismServices = new List<PrismServiceEntity>();
            foreach (var updateDidAction in request.UpdateDidActions)
            {
                if (updateDidAction.UpdateDidActionType == UpdateDidActionType.AddKey)
                {
                    prismPublicKeysToAdd.Add(new PrismPublicKeyEntity()
                    {
                        KeyId = updateDidAction.PrismPublicKey!.KeyId,
                        PublicKey = updateDidAction.PrismPublicKey.LongByteArray,
                        PrismKeyUsage = updateDidAction.PrismPublicKey.KeyUsage,
                        Curve = updateDidAction.PrismPublicKey.Curve,
                        UpdateOperationOrder = operationOrderIndex,
                    });
                }
                else if (updateDidAction.UpdateDidActionType == UpdateDidActionType.RemoveKey)
                {
                    prismPublicKeysToRemove.Add(new PrismPublicKeyRemoveEntity()
                    {
                        KeyId = updateDidAction.RemovedKeyId!,
                        UpdateOperationOrder = operationOrderIndex
                    });
                }
                else if (updateDidAction.UpdateDidActionType == UpdateDidActionType.AddService)
                {
                    prismServices.Add(new PrismServiceEntity()
                    {
                        ServiceId = updateDidAction.PrismService!.ServiceId,
                        Type = updateDidAction.PrismService.Type,
                        Uri = updateDidAction.PrismService.ServiceEndpoints.Uri,
                        ListOfUris = updateDidAction.PrismService.ServiceEndpoints.ListOfUris,
                        JsonData = updateDidAction.PrismService.ServiceEndpoints.Json is not null ? JsonSerializer.Serialize(updateDidAction.PrismService.ServiceEndpoints.Json) : null,
                        UpdateOperationOrder = operationOrderIndex,
                        Updated = false,
                        Removed = false
                    });
                }
                else if (updateDidAction.UpdateDidActionType == UpdateDidActionType.UpdateService)
                {
                    prismServices.Add(new PrismServiceEntity()
                    {
                        ServiceId = updateDidAction.PrismService!.ServiceId,
                        Type = updateDidAction.PrismService.Type,
                        Uri = updateDidAction.PrismService.ServiceEndpoints.Uri,
                        ListOfUris = updateDidAction.PrismService.ServiceEndpoints.ListOfUris,
                        JsonData = updateDidAction.PrismService.ServiceEndpoints.Json is not null ? JsonSerializer.Serialize(updateDidAction.PrismService.ServiceEndpoints.Json) : null,
                        UpdateOperationOrder = operationOrderIndex,
                        Updated = true,
                        Removed = false
                    });
                }
                else if (updateDidAction.UpdateDidActionType == UpdateDidActionType.RemoveService)
                {
                    prismServices.Add(new PrismServiceEntity()
                    {
                        ServiceId = updateDidAction.RemovedKeyId,
                        Type = null,
                        Uri = null,
                        ListOfUris = null,
                        JsonData = null,
                        UpdateOperationOrder = operationOrderIndex,
                        Updated = false,
                        Removed = true
                    });
                }
                else if (updateDidAction.UpdateDidActionType == UpdateDidActionType.PatchContext)
                {
                    prismServices.Add(new PrismServiceEntity()
                    {
                        ServiceId = updateDidAction.RemovedKeyId,
                        Type = null,
                        Uri = null,
                        ListOfUris = null,
                        JsonData = null,
                        UpdateOperationOrder = operationOrderIndex,
                        Updated = false,
                        Removed = true
                    });
                }

                operationOrderIndex++;
            }

            var prefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value);
            var hasExistingTransaction = await _context.TransactionEntities.AnyAsync(p => p.TransactionHash == request.TransactionHash.Value, cancellationToken: cancellationToken);
            if (!hasExistingTransaction)
            {
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
                    UpdateDidEntities = new List<UpdateDidEntity>()
                    {
                        new UpdateDidEntity()
                        {
                            OperationHash = request.OperationHash.Value,
                            OperationSequenceNumber = request.OperationSequenceNumber,
                            PreviousOperationHash = request.PreviousOperationHash.Value,
                            Did = PrismEncoding.HexToByteArray(request.Did),
                            SigningKeyId = request.SigningKeyId,
                            PrismPublicKeysToAdd = prismPublicKeysToAdd,
                            PrismPublicKeysToRemove = prismPublicKeysToRemove,
                            PrismServices = prismServices
                        }
                    }
                };
                await _context.TransactionEntities.AddAsync(trans, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return Result.Ok();
            }
            else
            {
                var prismUpdateDidEntity =
                    new UpdateDidEntity()
                    {
                        TransactionHash = request.TransactionHash.Value,
                        OperationHash = request.OperationHash.Value,
                        OperationSequenceNumber = request.OperationSequenceNumber,
                        BlockHeight = request.BlockHeight,
                        BlockHashPrefix = prefix!.Value,
                        PreviousOperationHash = request.PreviousOperationHash.Value,
                        Did = PrismEncoding.HexToByteArray(request.Did),
                        SigningKeyId = request.SigningKeyId,
                        PrismPublicKeysToAdd = prismPublicKeysToAdd,
                        PrismPublicKeysToRemove = prismPublicKeysToRemove,
                        PrismServices = prismServices
                    };
                await _context.UpdateDidEntities.AddAsync(prismUpdateDidEntity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail($"Invalid operation when saving a UpdateDid-Operation: blockHeight : '{request.BlockHeight}'. Message: {e.Message} Inner: {e.InnerException?.Message}");
        }
    }
}