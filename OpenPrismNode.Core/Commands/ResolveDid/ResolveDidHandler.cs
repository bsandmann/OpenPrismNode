namespace OpenPrismNode.Core.Commands.ResolveDid;

using System.Text.Json;
using Common;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using ResultSets;

public class ResolveDidHandler : IRequestHandler<ResolveDidRequest, Result<ResolveDidResponse>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    public ResolveDidHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Resolve a Did up to a specific point of with all available data
    /// Resolving up to a point in time allows re-parsing specific blocks or epochs in the past without accessing
    /// data related to did with was added later
    /// Resolving without any boundaries should only be done when resolving a did from a new operation on the top
    /// of the blockchain.
    /// Resolving should usally not fail, but just ignore operations with are inconsistent with the rules
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<ResolveDidResponse>> Handle(ResolveDidRequest request, CancellationToken cancellationToken)
    {
        byte[] didKey;
        try
        {
            didKey = PrismEncoding.HexToByteArray(request.DidIdentifier);
        }
        catch (Exception e)
        {
            return Result.Fail("The didIdentifier is not valid");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        context.ChangeTracker.Clear();
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        CreateDidResult? createDidResult;
        if (request.BlockHeight == null)
        {
            createDidResult = await context.CreateDidEntities
                .Where(p => p.TransactionEntity.BlockEntity.EpochEntity.Ledger == request.Ledger)
                .Select(p => new CreateDidResult()
                {
                    OperationHash = p.OperationHash,
                    OperationSequenceNumber = p.OperationSequenceNumber,
                    TransactionHash = p.TransactionEntity.TransactionHash,
                    BlockHeight = p.TransactionEntity.BlockHeight,
                    Index = p.TransactionEntity.Index,
                    TimeUtc = p.TransactionEntity.BlockEntity.TimeUtc,
                    PublicKeys = p.PrismPublicKeys.Select(q => new PublicKeyResult()
                    {
                        KeyId = q.KeyId,
                        PrismKeyUsage = q.PrismKeyUsage,
                        Curve = q.Curve,
                        PublicKey = q.PublicKey
                    }).ToList(),
                    Services = p.PrismServices.Select(r => new ServiceResult()
                    {
                        ServiceId = r.ServiceId,
                        Type = r.Type,
                        Uri = r.Uri,
                        ListOfUris = r.ListOfUris,
                        JsonData = r.JsonData,
                        Removed = r.Removed,
                        Updated = r.Updated,
                        UpdateOperationOrder = r.UpdateOperationOrder
                    }).ToList(),
                    PatchedContexts = p.PatchedContext != null ? p.PatchedContext.ContextList : null,
                    DeactivateDid = p.DidDeactivation == null
                        ? null
                        : new DeactivateDidInCreateDidResult()
                        {
                            OperationHash = p.DidDeactivation.OperationHash,
                            TransactionHash = p.DidDeactivation.TransactionHash,
                            BlockHeight = p.DidDeactivation.TransactionEntity.BlockHeight,
                            Index = p.DidDeactivation.TransactionEntity.Index,
                            OperationSequenceNumber = p.DidDeactivation.OperationSequenceNumber,
                            TimeUtc = p.DidDeactivation.TransactionEntity.BlockEntity.TimeUtc,
                        }
                })
                .FirstOrDefaultAsync(p => p.OperationHash == didKey, cancellationToken: cancellationToken);
        }
        else
        {
            createDidResult = await context.CreateDidEntities
                .Where(p => p.TransactionEntity.BlockEntity.EpochEntity.Ledger == request.Ledger)
                .Select(p => new CreateDidResult()
                {
                    OperationHash = p.OperationHash,
                    OperationSequenceNumber = p.OperationSequenceNumber,
                    TransactionHash = p.TransactionEntity.TransactionHash,
                    BlockHeight = p.TransactionEntity.BlockHeight,
                    Index = p.TransactionEntity.Index,
                    TimeUtc = p.TransactionEntity.BlockEntity.TimeUtc,
                    PublicKeys = p.PrismPublicKeys.Select(q => new PublicKeyResult()
                    {
                        KeyId = q.KeyId,
                        PrismKeyUsage = q.PrismKeyUsage,
                        Curve = q.Curve,
                        PublicKey = q.PublicKey
                    }).ToList(),
                    Services = p.PrismServices.Select(r => new ServiceResult()
                    {
                        ServiceId = r.ServiceId,
                        Type = r.Type,
                        Uri = r.Uri,
                        ListOfUris = r.ListOfUris,
                        JsonData = r.JsonData,
                        Removed = r.Removed,
                        Updated = r.Updated,
                        UpdateOperationOrder = r.UpdateOperationOrder
                    }).ToList(),
                    PatchedContexts = p.PatchedContext != null ? p.PatchedContext.ContextList : null,
                    DeactivateDid = p.DidDeactivation == null
                        ? null
                        : new DeactivateDidInCreateDidResult()
                        {
                            OperationHash = p.DidDeactivation.OperationHash,
                            TransactionHash = p.DidDeactivation.TransactionHash,
                            BlockHeight = p.DidDeactivation.TransactionEntity.BlockHeight,
                            Index = p.DidDeactivation.TransactionEntity.Index,
                            OperationSequenceNumber = p.DidDeactivation.OperationSequenceNumber,
                            TimeUtc = p.DidDeactivation.TransactionEntity.BlockEntity.TimeUtc,
                        }
                })
                .FirstOrDefaultAsync(q => q.OperationHash == didKey &&
                                          ((q.BlockHeight < request.BlockHeight) ||
                                           (q.BlockHeight == request.BlockHeight && q.Index < request.BlockSequence) ||
                                           (q.BlockHeight == request.BlockHeight && q.Index == request.BlockSequence && q.OperationSequenceNumber < request.OperationSequence)), cancellationToken: cancellationToken);
        }

        if (createDidResult is null)
        {
            return Result.Fail($"Did could not be found on the {request.Ledger} ledger");
        }


        if (request.BlockHeight is not null && createDidResult.DeactivateDid is not null)
        {
            var deactivateDidIsInsideTimeWindow = (createDidResult.DeactivateDid.BlockHeight < request.BlockHeight) ||
                                                  (createDidResult.DeactivateDid.BlockHeight == request.BlockHeight && createDidResult.DeactivateDid.Index < request.BlockSequence) ||
                                                  (createDidResult.DeactivateDid.BlockHeight == request.BlockHeight && createDidResult.DeactivateDid.Index == request.BlockSequence && createDidResult.DeactivateDid.OperationSequenceNumber < request.OperationSequence);
            if (!deactivateDidIsInsideTimeWindow)
            {
                // The deactivateDid operation was applied after the timewindow we are searching in. So we ignore it
                createDidResult.DeactivateDid = null;
            }
        }

        var resolved = ResolveCreateDidOperation(request, createDidResult);
        var lastOperationHash = createDidResult.OperationHash;

        List<UpdateDidResult> updateOperations = new List<UpdateDidResult>();
        if (request.BlockHeight == null)
        {
            updateOperations = await context.UpdateDidEntities
                .Where(p => p.TransactionEntity.BlockEntity.EpochEntity.Ledger == request.Ledger)
                .Select(p => new UpdateDidResult()
                {
                    Did = p.Did,
                    OperationHash = p.OperationHash,
                    OperationSequenceNumber = p.OperationSequenceNumber,
                    TransactionHash = p.TransactionEntity.TransactionHash,
                    BlockHeight = p.TransactionEntity.BlockHeight,
                    Index = p.TransactionEntity.Index,
                    TimeUtc = p.TransactionEntity.BlockEntity.TimeUtc,
                    PublicKeysToAdd = p.PrismPublicKeysToAdd.Select(q => new PublicKeyResult()
                    {
                        KeyId = q.KeyId,
                        Curve = q.Curve,
                        PrismKeyUsage = q.PrismKeyUsage,
                        PublicKey = q.PublicKey,
                        UpdateOperationOrder = q.UpdateOperationOrder,
                    }).ToList(),
                    PublicKeysToRemove = p.PrismPublicKeysToRemove.Select(q => new Tuple<string, int>(q.KeyId, q.UpdateOperationOrder)).ToList(),
                    Services = p.PrismServices.Select(r => new ServiceResult()
                    {
                        ServiceId = r.ServiceId,
                        Type = r.Type,
                        Uri = r.Uri,
                        ListOfUris = r.ListOfUris,
                        JsonData = r.JsonData,
                        Removed = r.Removed,
                        Updated = r.Updated,
                        UpdateOperationOrder = r.UpdateOperationOrder
                    }).ToList(),
                    PatchedContextResults = p.PatchedContexts.Select(q => new PatchedContextResult()
                    {
                        ContextList = q.ContextList,
                        UpdateOperationOrder = q.UpdateOperationOrder
                    }).ToList(),
                })
                .Where(d => d.Did == didKey)
                .ToListAsync(cancellationToken: cancellationToken);
        }
        else
        {
            // We don't need to restrict the query towards the lower bound, because an update DID operation always
            // requires an exisiting createDID operation, thus the update operation always comes after the createDid operation
            updateOperations = await context.UpdateDidEntities
                .Where(p => p.TransactionEntity.BlockEntity.EpochEntity.Ledger == request.Ledger)
                .Select(p => new UpdateDidResult()
                {
                    Did = p.Did,
                    OperationHash = p.OperationHash,
                    OperationSequenceNumber = p.OperationSequenceNumber,
                    TransactionHash = p.TransactionEntity.TransactionHash,
                    BlockHeight = p.TransactionEntity.BlockHeight,
                    Index = p.TransactionEntity.Index,
                    TimeUtc = p.TransactionEntity.BlockEntity.TimeUtc,
                    PublicKeysToAdd = p.PrismPublicKeysToAdd.Select(q => new PublicKeyResult()
                    {
                        KeyId = q.KeyId,
                        Curve = q.Curve,
                        PrismKeyUsage = q.PrismKeyUsage,
                        PublicKey = q.PublicKey,
                        UpdateOperationOrder = q.UpdateOperationOrder,
                    }).ToList(),
                    PublicKeysToRemove = p.PrismPublicKeysToRemove.Select(q => new Tuple<string, int>(q.KeyId, q.UpdateOperationOrder)).ToList(),
                    Services = p.PrismServices.Select(r => new ServiceResult()
                    {
                        ServiceId = r.ServiceId,
                        Type = r.Type,
                        Uri = r.Uri,
                        ListOfUris = r.ListOfUris,
                        JsonData = r.JsonData,
                        Removed = r.Removed,
                        Updated = r.Updated,
                        UpdateOperationOrder = r.UpdateOperationOrder
                    }).ToList(),
                    PatchedContextResults = p.PatchedContexts.Select(q => new PatchedContextResult()
                    {
                        ContextList = q.ContextList,
                        UpdateOperationOrder = q.UpdateOperationOrder
                    }).ToList(),
                })
                .Where(q => q.Did == didKey &&
                            ((q.BlockHeight < request.BlockHeight) ||
                             (q.BlockHeight == request.BlockHeight && q.Index < request.BlockSequence) ||
                             (q.BlockHeight == request.BlockHeight && q.Index == request.BlockSequence && q.OperationSequenceNumber < request.OperationSequence)))
                .ToListAsync(cancellationToken: cancellationToken);
        }

        if (updateOperations.Any())
        {
            var prismPublicKeys = new List<PrismPublicKey>();
            var prismServices = new List<PrismService>();
            var patchedContexts = resolved.Contexts;
            prismPublicKeys.AddRange(resolved.PublicKeys);
            prismServices.AddRange(resolved.PrismServices);
            var orderedUpdateOperations = OrderUpdateOperations(updateOperations);
            foreach (var updateOperation in orderedUpdateOperations)
            {
                lastOperationHash = updateOperation.OperationHash;
                var addActionsIndex = updateOperation.PublicKeysToAdd.Count;
                var removeActionsIndex = updateOperation.PublicKeysToRemove.Count;
                var servicesIndex = updateOperation.Services.Count;
                var patchedContextsIndex = updateOperation.PatchedContextResults.Count;
                var combinedIndexLength = addActionsIndex + removeActionsIndex + servicesIndex + patchedContextsIndex;
                for (int i = 0; i < combinedIndexLength; i++)
                {
                    var addKeyAction = updateOperation.PublicKeysToAdd.FirstOrDefault(p => p.UpdateOperationOrder == i);
                    var removeKeyAction = updateOperation.PublicKeysToRemove.FirstOrDefault(p => p.Item2 == i);
                    var addServiceAction = updateOperation.Services.FirstOrDefault(p => p.UpdateOperationOrder == i && !p.Removed && !p.Updated);
                    var updateServiceAction = updateOperation.Services.FirstOrDefault(p => p.UpdateOperationOrder == i && p.Updated);
                    var removeServiceAction = updateOperation.Services.FirstOrDefault(p => p.UpdateOperationOrder == i && p.Removed);
                    var patchedContextAction = updateOperation.PatchedContextResults.FirstOrDefault(p => p.UpdateOperationOrder == i);
                    if (addKeyAction is not null)
                    {
                        if (resolved.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(addKeyAction.KeyId, StringComparison.InvariantCultureIgnoreCase)) is not null)
                        {
                            // should never happen
                            return Result.Fail("Fatal error. The key which should be added already exists");
                        }
                        else
                        {
                            var keysXy = PrismEncoding.HexToPublicKeyPairByteArrays(PrismEncoding.ByteArrayToHex(addKeyAction.PublicKey));
                            if (keysXy.IsFailed)
                            {
                                return keysXy.ToResult();
                            }

                            var (keyX, keyY) = keysXy.Value;

                            if (addKeyAction.Curve.Equals(PrismParameters.Secp256k1CurveName))
                            {
                                prismPublicKeys.Add(new PrismPublicKey(
                                    keyUsage: addKeyAction.PrismKeyUsage,
                                    keyId: addKeyAction.KeyId,
                                    curve: addKeyAction.Curve,
                                    x: keyX,
                                    y: keyY.Length == 0 ? null : keyY
                                ));
                            }
                            else
                            {
                                prismPublicKeys.Add(new PrismPublicKey(
                                    keyUsage: addKeyAction.PrismKeyUsage,
                                    keyId: addKeyAction.KeyId,
                                    curve: addKeyAction.Curve,
                                    rawBytes: keyX
                                ));
                            }
                        }
                    }
                    else if (removeKeyAction is not null)
                    {
                        var keyToRemove = prismPublicKeys.FirstOrDefault(p => p.KeyId.Equals(removeKeyAction.Item1, StringComparison.InvariantCultureIgnoreCase));
                        if (keyToRemove is null)
                        {
                            // should never happen
                            return Result.Fail("Fatal error. The key which should be removed does not exist");
                        }

                        prismPublicKeys.Remove(keyToRemove);
                    }
                    else if (addServiceAction is not null)
                    {
                        if (resolved.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(addServiceAction.ServiceId, StringComparison.InvariantCultureIgnoreCase)) is not null)
                        {
                            // should never happen
                            return Result.Fail("Fatal error. The service which should be added already exists");
                        }
                        else
                        {
                            prismServices.Add(new PrismService(
                                serviceId: addServiceAction.ServiceId,
                                type: addServiceAction.Type,
                                serviceEndpoints: new ServiceEndpoints()
                                {
                                    Uri = addServiceAction.Uri,
                                    ListOfUris = addServiceAction.ListOfUris,
                                    Json = addServiceAction.JsonData is not null ? JsonSerializer.Deserialize<Dictionary<string, object>>(addServiceAction.JsonData) : null
                                }));
                        }
                    }
                    else if (updateServiceAction is not null)
                    {
                        var serviceToUpdate = prismServices.FirstOrDefault(p => p.ServiceId.Equals(updateServiceAction.ServiceId, StringComparison.InvariantCultureIgnoreCase));
                        if (serviceToUpdate is null)
                        {
                            // should never happen
                            return Result.Fail("Fatal error. The service which should be updated does not exist");
                        }

                        serviceToUpdate.Type = updateServiceAction.Type;
                        serviceToUpdate.ServiceEndpoints = new ServiceEndpoints()
                        {
                            Uri = updateServiceAction.Uri,
                            ListOfUris = updateServiceAction.ListOfUris,
                            Json = updateServiceAction.JsonData is not null ? JsonSerializer.Deserialize<Dictionary<string, object>>(updateServiceAction.JsonData) : null
                        };
                    }
                    else if (removeServiceAction is not null)
                    {
                        var serviceToRemove = prismServices.FirstOrDefault(p => p.ServiceId.Equals(removeServiceAction.ServiceId, StringComparison.InvariantCultureIgnoreCase));
                        if (serviceToRemove is null)
                        {
                            // should never happen
                            return Result.Fail("Fatal error. The service which should be removed does not exist");
                        }

                        prismServices.Remove(serviceToRemove);
                    }
                    else if (patchedContextAction is not null)
                    {
                        if (!patchedContextAction.ContextList.Any())
                        {
                            patchedContexts = patchedContexts.Where(p => p.Equals(PrismParameters.JsonLdDefaultContext) ||
                                                                         p.Equals(PrismParameters.JsonLdJsonWebKey2020) ||
                                                                         p.Equals(PrismParameters.JsonLdDidCommMessaging) ||
                                                                         p.Equals(PrismParameters.JsonLdLinkedDomains)).ToList();
                        }
                        else
                        {
                            patchedContexts.AddRange(patchedContextAction.ContextList);
                        }
                    }
                    else
                    {
                        throw new Exception("Operation index error in database");
                    }

                    resolved.Updated = DateTime.SpecifyKind(updateOperation.TimeUtc, DateTimeKind.Utc);
                    resolved.VersionId = PrismEncoding.ByteArrayToHex(updateOperation.OperationHash);
                    resolved.CardanoTransactionPosition = updateOperation.Index;
                    resolved.OperationPosition = updateOperation.OperationSequenceNumber;
                    resolved.UpdateTxId = PrismEncoding.ByteArrayToHex(updateOperation.TransactionHash);
                }
            }

            resolved.PublicKeys.Clear();
            resolved.PublicKeys.AddRange(prismPublicKeys.OrderBy(p => p.KeyId));
            resolved.PrismServices.Clear();
            resolved.PrismServices.AddRange(prismServices.OrderBy(p => p.ServiceId));
            resolved.Contexts = patchedContexts.Distinct().ToList();
        }

        // lastly we have to apply the deactivateDid operation
        if (createDidResult.DeactivateDid is not null)
        {
            resolved.PublicKeys.Clear();
            resolved.PrismServices.Clear();
            resolved.Contexts.Clear();
            resolved.Deactivated = true;
            resolved.Updated = DateTime.SpecifyKind(createDidResult.DeactivateDid.TimeUtc, DateTimeKind.Utc);
            resolved.VersionId = PrismEncoding.ByteArrayToHex(createDidResult.DeactivateDid.OperationHash);
            resolved.CardanoTransactionPosition = createDidResult.DeactivateDid.Index;
            resolved.OperationPosition = createDidResult.DeactivateDid.OperationSequenceNumber;
            resolved.UpdateTxId = PrismEncoding.ByteArrayToHex(createDidResult.DeactivateDid.TransactionHash);
            resolved.DeactivateTxId = PrismEncoding.ByteArrayToHex(createDidResult.DeactivateDid.TransactionHash);
        }

        var resolveDidResponse = new ResolveDidResponse(resolved, Hash.CreateFrom(lastOperationHash));

        return Result.Ok(resolveDidResponse);
    }

    private static InternalDidDocument ResolveCreateDidOperation(ResolveDidRequest request, CreateDidResult createDidResult)
    {
        var returnDocument = new InternalDidDocument(
            request.DidIdentifier,
            publicKeys: new List<PrismPublicKey>(),
            prismServices: new List<PrismService>(),
            contexts: new List<string>() { PrismParameters.JsonLdDefaultContext },
            created: DateTime.SpecifyKind(createDidResult.TimeUtc, DateTimeKind.Utc),
            updated: null,
            versionId: PrismEncoding.ByteArrayToHex(createDidResult.OperationHash),
            cardanoTransactionPosition: createDidResult.Index,
            operationPosition: createDidResult.OperationSequenceNumber,
            originTxId: PrismEncoding.ByteArrayToHex(createDidResult.TransactionHash),
            updateTxId: null,
            deactivateTxId: null
        );
        var prismPublicKeys = new List<PrismPublicKey>();
        var prismServices = new List<PrismService>();
        foreach (var prismPublicKeyEntity in createDidResult.PublicKeys)
        {
            var keyId = prismPublicKeyEntity.KeyId;
            var publicKeyLongFormBytes = prismPublicKeyEntity.PublicKey;
            var keysXy = PrismEncoding.HexToPublicKeyPairByteArrays(PrismEncoding.ByteArrayToHex(publicKeyLongFormBytes));
            var (keyX, keyY) = keysXy.Value;
            var keyUsage = prismPublicKeyEntity.PrismKeyUsage;
            if (prismPublicKeyEntity.Curve.Equals(PrismParameters.Secp256k1CurveName))
            {
                prismPublicKeys.Add(new PrismPublicKey(
                    keyUsage: keyUsage,
                    keyId: keyId!,
                    curve: prismPublicKeyEntity.Curve,
                    x: keyX,
                    y: keyY.Length == 0 ? null : keyY
                ));
            }
            else
            {
                prismPublicKeys.Add(new PrismPublicKey(
                    keyUsage: keyUsage,
                    keyId: keyId!,
                    curve: prismPublicKeyEntity.Curve,
                    rawBytes: keyX
                ));
            }
        }

        if (prismPublicKeys.Any(p => p.KeyUsage != PrismKeyUsage.MasterKey))
        {
            returnDocument.Contexts.Add(PrismParameters.JsonLdJsonWebKey2020);
        }

        foreach (var service in createDidResult.Services)
        {
            prismServices.Add(new PrismService(
                serviceId: service.ServiceId,
                type: service.Type,
                serviceEndpoints: new ServiceEndpoints()
                {
                    Uri = service.Uri,
                    Json = service.JsonData is not null ? JsonSerializer.Deserialize<Dictionary<string, object>>(service.JsonData) : null,
                    ListOfUris = service.ListOfUris,
                }
            ));
        }

        if (prismServices.Any(p => p.Type.Equals(PrismParameters.ServiceTypeDIDCommMessaging)))
        {
            returnDocument.Contexts.Add(PrismParameters.JsonLdDidCommMessaging);
        }

        if (prismServices.Any(p => p.Type.Equals(PrismParameters.ServiceTypeLinkedDomains)))
        {
            returnDocument.Contexts.Add(PrismParameters.JsonLdLinkedDomains);
        }

        returnDocument.Contexts.AddRange(createDidResult.PatchedContexts ?? new List<string>());
        returnDocument.Contexts = returnDocument.Contexts.Distinct().ToList();
        returnDocument.PublicKeys.AddRange(prismPublicKeys.OrderBy(p => p.KeyId));
        returnDocument.PrismServices.AddRange(prismServices);

        return returnDocument;
    }

    /// <summary>
    /// Method to order UpdateDid Operations in the correct order beginning from the past up to the present
    /// </summary>
    /// <param name="updateDidResults"></param>
    /// <returns></returns>
    private static List<UpdateDidResult> OrderUpdateOperations(List<UpdateDidResult> updateDidResults)
    {
        var finalOrderdList = new List<UpdateDidResult>();
        var groupedByBlockHeightList = updateDidResults.GroupBy(p => p.BlockHeight).OrderBy(p => p.Key).ToList();
        foreach (var groupedByBlockHeight in groupedByBlockHeightList)
        {
            var groupedByBlockSequenceList = groupedByBlockHeight.GroupBy(p => p.Index).OrderBy(p => p.Key).ToList();
            foreach (var groupedByBlockSequence in groupedByBlockSequenceList)
            {
                var groupedByOperationSequence = groupedByBlockSequence.OrderBy(p => p.OperationSequenceNumber).ToList();
                finalOrderdList.AddRange(groupedByOperationSequence);
            }
        }

        return finalOrderdList;
    }
}