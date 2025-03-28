﻿namespace OpenPrismNode.Core.Commands.CreateTransactionUpdateDid;

using System.Text.Json;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

/// <summary>
/// Handler to write a UpdateDid-PRISM-Operation in the node database
/// </summary>
public class CreateTransactionUpdateDidHandler : IRequestHandler<CreateTransactionUpdateDidRequest, Result>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    public CreateTransactionUpdateDidHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(CreateTransactionUpdateDidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            context.ChangeTracker.Clear();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var operationOrderIndex = 0;
            var prismPublicKeysToAdd = new List<PrismPublicKeyEntity>();
            var prismPublicKeysToRemove = new List<PrismPublicKeyRemoveEntity>();
            var prismServices = new List<PrismServiceEntity>();
            var patchedContexts = new List<PatchedContextEntity>();
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
                    patchedContexts.Add(new PatchedContextEntity()
                    {
                        ContextList = updateDidAction.Contexts is not null ? updateDidAction.Contexts : new List<string>(),
                        UpdateOperationOrder = operationOrderIndex,
                    });
                }

                operationOrderIndex++;
            }

            var prefix = BlockEntity.CalculateBlockHashPrefix(request.BlockHash.Value);
            var hasExistingTransaction = await context.TransactionEntities.AnyAsync(p => p.TransactionHash == request.TransactionHash.Value, cancellationToken: cancellationToken);
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
                            PrismServices = prismServices,
                            PatchedContexts = patchedContexts,
                        }
                    }
                };
                await context.TransactionEntities.AddAsync(trans, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

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
                        PrismServices = prismServices,
                        PatchedContexts = patchedContexts
                    };
                await context.UpdateDidEntities.AddAsync(prismUpdateDidEntity, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail($"Invalid operation when saving a UpdateDid-Operation: blockHeight : '{request.BlockHeight}'. Message: {e.Message} Inner: {e.InnerException?.Message}");
        }
    }
}