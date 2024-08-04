namespace OpenPrismNode.Core.Commands.ResolveDid;

using FluentResults;
using MediatR;

// using Blocktrust.Domain;
// using Blocktrust.Domain.Helper;
// using FluentResults;
// using GetTransactionByHash.ResultSets;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using OpenPrismNode.Core.Commands.ResolveDid;
// using ResultSets;

public class ResolveDidHandler : IRequestHandler<ResolveDidRequest, Result<ResolveDidResponse>>
{
    // private readonly DataContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context"></param>
    // public ResolveDidHandler(DataContext context)
    // {
    //     this._context = context;
    // }

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
    //     var didKey = PrismEncoding.HexToByteArray(request.Did.Identifier);
    //     CreateDidResult? createDidResult;
    //     if (request.BlockHeight == null)
    //     {
    //         createDidResult = await _context.PrismCreateDidEntities.Select(p => new CreateDidResult()
    //             {
    //                 OperationHash = p.OperationHash,
    //                 TransactionHash = p.PrismTransactionEntity.TransactionHash,
    //                 TimeUtc = p.PrismTransactionEntity.PrismBlockEntity.TimeUtc,
    //                 PublicKeys = p.PrismPublicKeys.Select(q => new PublicKeyResult()
    //                 {
    //                     KeyId = q.KeyId,
    //                     PrismKeyUsage = q.PrismKeyUsage,
    //                     PublicKey = q.PublicKey
    //                 }).ToList(),
    //                 Services = p.PrismServices.Select(r => new ServiceResult()
    //                 {
    //                     ServiceId = r.ServiceId,
    //                     SerivceEndpoints = r.ServiceEndpoints,
    //                     Type = r.Type,
    //                     Removed = r.Removed,
    //                     Updated = r.Updated,
    //                     UpdateOperationOrder = r.UpdateOperationOrder
    //                 }).ToList(),
    //                 LedgerType = p.PrismTransactionEntity.PrismBlockEntity.PrismEpochEntity.NetworkType,
    //                 Index = p.PrismTransactionEntity.Index,
    //                 OperationSequenceNumber = p.OperationSequenceNumber,
    //                 DeactivateDid = p.DidDeactivation == null
    //                     ? null
    //                     : new DeactivateDidInCreateDidResult()
    //                     {
    //                         OperationHash = p.DidDeactivation.OperationHash,
    //                         TransactionHash = p.DidDeactivation.TransactionHash,
    //                         BlockHeight = p.DidDeactivation.PrismTransactionEntity.PrismBlockEntity.BlockHeight,
    //                         Index = p.DidDeactivation.PrismTransactionEntity.Index,
    //                         OperationSequenceNumber = p.DidDeactivation.OperationSequenceNumber,
    //                         TimeUtc = p.DidDeactivation.PrismTransactionEntity.PrismBlockEntity.TimeUtc,
    //                         LedgerType = p.DidDeactivation.PrismTransactionEntity.PrismBlockEntity.PrismEpochEntity.NetworkType,
    //                     }
    //             })
    //             .FirstOrDefaultAsync(p => p.OperationHash == didKey, cancellationToken: cancellationToken);
    //     }
    //     else
    //     {
    //         createDidResult = await _context.PrismCreateDidEntities.Select(p => new CreateDidResult()
    //             {
    //                 OperationHash = p.OperationHash,
    //                 TransactionHash = p.PrismTransactionEntity.TransactionHash,
    //                 TimeUtc = p.PrismTransactionEntity.PrismBlockEntity.TimeUtc,
    //                 PublicKeys = p.PrismPublicKeys.Select(q => new PublicKeyResult()
    //                 {
    //                     KeyId = q.KeyId,
    //                     PrismKeyUsage = q.PrismKeyUsage,
    //                     PublicKey = q.PublicKey
    //                 }).ToList(),
    //                 Services = p.PrismServices.Select(r => new ServiceResult()
    //                 {
    //                     ServiceId = r.ServiceId,
    //                     SerivceEndpoints = r.ServiceEndpoints,
    //                     Type = r.Type,
    //                     Removed = r.Removed,
    //                     Updated = r.Updated,
    //                     UpdateOperationOrder = r.UpdateOperationOrder
    //                 }).ToList(),
    //                 LedgerType = p.PrismTransactionEntity.PrismBlockEntity.PrismEpochEntity.NetworkType,
    //                 BlockHeight = p.PrismTransactionEntity.PrismBlockEntity.BlockHeight,
    //                 Index = p.PrismTransactionEntity.Index,
    //                 OperationSequenceNumber = p.OperationSequenceNumber,
    //                 DeactivateDid = p.DidDeactivation == null
    //                     ? null
    //                     : new DeactivateDidInCreateDidResult()
    //                     {
    //                         OperationHash = p.DidDeactivation.OperationHash,
    //                         TransactionHash = p.DidDeactivation.TransactionHash,
    //                         BlockHeight = p.DidDeactivation.PrismTransactionEntity.PrismBlockEntity.BlockHeight,
    //                         Index = p.DidDeactivation.PrismTransactionEntity.Index,
    //                         OperationSequenceNumber = p.DidDeactivation.OperationSequenceNumber,
    //                         TimeUtc = p.DidDeactivation.PrismTransactionEntity.PrismBlockEntity.TimeUtc,
    //                         LedgerType = p.DidDeactivation.PrismTransactionEntity.PrismBlockEntity.PrismEpochEntity.NetworkType,
    //                     }
    //             })
    //             .FirstOrDefaultAsync(q => q.OperationHash == didKey &&
    //                                       ((q.BlockHeight < request.BlockHeight) ||
    //                                        (q.BlockHeight == request.BlockHeight && q.Index < request.BlockSequence) ||
    //                                        (q.BlockHeight == request.BlockHeight && q.Index == request.BlockSequence && q.OperationSequenceNumber < request.OperationSequence)), cancellationToken: cancellationToken);
    //     }
    //
    //     if (createDidResult is null)
    //     {
    //         return Result.Fail("Did could not be found on the blockchain");
    //     }
    //
    //     if (request.BlockHeight is not null && createDidResult.DeactivateDid is not null)
    //     {
    //         var deactivateDidIsInsideTimeWindow = (createDidResult.DeactivateDid.BlockHeight < request.BlockHeight) ||
    //                                               (createDidResult.DeactivateDid.BlockHeight == request.BlockHeight && createDidResult.DeactivateDid.Index < request.BlockSequence) ||
    //                                               (createDidResult.DeactivateDid.BlockHeight == request.BlockHeight && createDidResult.DeactivateDid.Index == request.BlockSequence && createDidResult.DeactivateDid.OperationSequenceNumber < request.OperationSequence);
    //         if (!deactivateDidIsInsideTimeWindow)
    //         {
    //             // The deactivateDid operation was applied after the timewindow we are searching in. So we ignore it
    //             createDidResult.DeactivateDid = null;
    //         }
    //     }
    //
    //     var resolved = ResolveCreateDidOperation(request, createDidResult);
    //     var lastOperationHash = createDidResult.OperationHash;
    //
    //     List<UpdateDidResult> updateOperations = new List<UpdateDidResult>();
    //     if (request.BlockHeight == null)
    //     {
    //         updateOperations = await _context.PrismUpdateDidEntities
    //             .Select(p => new UpdateDidResult()
    //             {
    //                 Did = p.Did,
    //                 OperationHash = p.OperationHash,
    //                 TransactionHash = p.PrismTransactionEntity.TransactionHash,
    //                 TimeUtc = p.PrismTransactionEntity.PrismBlockEntity.TimeUtc,
    //                 PublicKeysToAdd = p.PrismPublicKeysToAdd.Select(q => new PublicKeyResult()
    //                 {
    //                     KeyId = q.KeyId,
    //                     PrismKeyUsage = q.PrismKeyUsage,
    //                     PublicKey = q.PublicKey,
    //                     UpdateOperationOrder = q.UpdateOperationOrder
    //                 }).ToList(),
    //                 Services = p.PrismServices.Select(r => new ServiceResult()
    //                 {
    //                     ServiceId = r.ServiceId,
    //                     SerivceEndpoints = r.ServiceEndpoints,
    //                     Type = r.Type,
    //                     Removed = r.Removed,
    //                     Updated = r.Updated,
    //                     UpdateOperationOrder = r.UpdateOperationOrder
    //                 }).ToList(),
    //                 LedgerType = p.PrismTransactionEntity.PrismBlockEntity.PrismEpochEntity.NetworkType,
    //                 BlockHeight = p.PrismTransactionEntity.PrismBlockEntity.BlockHeight,
    //                 Index = p.PrismTransactionEntity.Index,
    //                 OperationSequenceNumber = p.OperationSequenceNumber,
    //                 PublicKeysToRemove = p.PrismPublicKeysToRemove.Select(q => new Tuple<string, int>(q.KeyId, q.UpdateOperationOrder)).ToList()
    //             })
    //             .Where(d => d.Did == didKey)
    //             .ToListAsync(cancellationToken: cancellationToken);
    //     }
    //     else
    //     {
    //         // We don't need to restrict the query towards the lower bound, because an update DID operation always
    //         // requires an exisiting createDID operation, thus the update operation always comes after the createDid operation
    //         updateOperations = await _context.PrismUpdateDidEntities
    //             .Select(p => new UpdateDidResult()
    //             {
    //                 Did = p.Did,
    //                 OperationHash = p.OperationHash,
    //                 TransactionHash = p.PrismTransactionEntity.TransactionHash,
    //                 TimeUtc = p.PrismTransactionEntity.PrismBlockEntity.TimeUtc,
    //                 PublicKeysToAdd = p.PrismPublicKeysToAdd.Select(q => new PublicKeyResult()
    //                 {
    //                     KeyId = q.KeyId,
    //                     PrismKeyUsage = q.PrismKeyUsage,
    //                     PublicKey = q.PublicKey,
    //                     UpdateOperationOrder = q.UpdateOperationOrder
    //                 }).ToList(),
    //                 Services = p.PrismServices.Select(r => new ServiceResult()
    //                 {
    //                     ServiceId = r.ServiceId,
    //                     SerivceEndpoints = r.ServiceEndpoints,
    //                     Type = r.Type,
    //                     Removed = r.Removed,
    //                     Updated = r.Updated,
    //                     UpdateOperationOrder = r.UpdateOperationOrder
    //                 }).ToList(),
    //                 LedgerType = p.PrismTransactionEntity.PrismBlockEntity.PrismEpochEntity.NetworkType,
    //                 BlockHeight = p.PrismTransactionEntity.PrismBlockEntity.BlockHeight,
    //                 Index = p.PrismTransactionEntity.Index,
    //                 OperationSequenceNumber = p.OperationSequenceNumber,
    //                 PublicKeysToRemove = p.PrismPublicKeysToRemove.Select(q => new Tuple<string, int>(q.KeyId, q.UpdateOperationOrder)).ToList()
    //             })
    //             .Where(q => q.Did == didKey &&
    //                         ((q.BlockHeight < request.BlockHeight) ||
    //                          (q.BlockHeight == request.BlockHeight && q.Index < request.BlockSequence) ||
    //                          (q.BlockHeight == request.BlockHeight && q.Index == request.BlockSequence && q.OperationSequenceNumber < request.OperationSequence)))
    //             .ToListAsync(cancellationToken: cancellationToken);
    //     }
    //
    //     if (updateOperations.Any())
    //     {
    //         var prismPublicKeys = new List<PrismPublicKey>();
    //         var prismServices = new List<PrismService>();
    //         prismPublicKeys.AddRange(resolved.PublicKeys);
    //         prismServices.AddRange(resolved.PrismServices);
    //
    //         var orderedUpdateOperations = OrderUpdateOperations(updateOperations);
    //         foreach (var updateOperation in orderedUpdateOperations)
    //         {
    //             lastOperationHash = updateOperation.OperationHash;
    //             var addActionsIndex = updateOperation.PublicKeysToAdd.Count;
    //             var removeActionsIndex = updateOperation.PublicKeysToRemove.Count;
    //             var servicesIndex = updateOperation.Services.Count;
    //             //TODO put the services also here??
    //             var combinedIndexLength = addActionsIndex + removeActionsIndex + servicesIndex;
    //             for (int i = 0; i < combinedIndexLength; i++)
    //             {
    //                 var addKeyAction = updateOperation.PublicKeysToAdd.FirstOrDefault(p => p.UpdateOperationOrder == i);
    //                 var removeKeyAction = updateOperation.PublicKeysToRemove.FirstOrDefault(p => p.Item2 == i);
    //                 var addServiceAction = updateOperation.Services.FirstOrDefault(p => p.UpdateOperationOrder == i && !p.Removed && !p.Updated);
    //                 var updateServiceAction = updateOperation.Services.FirstOrDefault(p => p.UpdateOperationOrder == i && p.Updated);
    //                 var removeServiceAction = updateOperation.Services.FirstOrDefault(p => p.UpdateOperationOrder == i && p.Removed);
    //                 if (addKeyAction is not null)
    //                 {
    //                     if (resolved.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(addKeyAction.KeyId, StringComparison.InvariantCultureIgnoreCase)) is not null)
    //                     {
    //                         // should never happen
    //                         return Result.Fail("Fatal error. The key which should be added already exists");
    //                     }
    //                     else
    //                     {
    //                         var keysXy = PrismEncoding.HexToPublicKeyPairByteArrays(PrismEncoding.ByteArrayToHex(addKeyAction.PublicKey));
    //                         var (keyX, keyY) = keysXy.Value;
    //                         prismPublicKeys.Add(new PrismPublicKey(
    //                             keyUsage: addKeyAction.PrismKeyUsage,
    //                             keyId: addKeyAction.KeyId!,
    //                             curve: addKeyAction.Curve,
    //                             keyX: keyX,
    //                         keyY:
    //                         keyY,
    //                         addedOn:
    //                         new PrismLedgerData(
    //                             transactionId: PrismEncoding.ByteArrayToHex(updateOperation.TransactionHash),
    //                             ledgerType: updateOperation.LedgerType,
    //                             timestampInfo: new LedgerTimestampInfo(
    //                                 blockSequenceNumber: (uint)updateOperation.Index,
    //                                 operationSequenceNumber: (uint)updateOperation.OperationSequenceNumber,
    //                                 blockTimestamp: updateOperation.TimeUtc
    //                             )),
    //                         revokedOn:
    //                         null
    //                             ));
    //                     }
    //                 }
    //                 else if (removeKeyAction is not null)
    //                 {
    //                     var keyToRemove = prismPublicKeys.FirstOrDefault(p => p.KeyId.Equals(removeKeyAction.Item1, StringComparison.InvariantCultureIgnoreCase));
    //                     if (keyToRemove is null)
    //                     {
    //                         // should never happen
    //                         return Result.Fail("Fatal error. The key which should be removed does not exist");
    //                     }
    //                     else if (keyToRemove.RevokedOn is not null)
    //                     {
    //                         // should never happen. The key was already revoked. Revoking the key another time does not change its revocation-date;
    //                         return Result.Fail("Fatal error. The key was already revoked");
    //                     }
    //                     else
    //                     {
    //                         keyToRemove.RevokedOn = new PrismLedgerData(
    //                             transactionId: PrismEncoding.ByteArrayToHex(updateOperation.TransactionHash),
    //                             ledgerType: updateOperation.LedgerType,
    //                             timestampInfo: new LedgerTimestampInfo(
    //                                 blockSequenceNumber: (uint)updateOperation.Index,
    //                                 operationSequenceNumber: (uint)updateOperation.OperationSequenceNumber,
    //                                 blockTimestamp: updateOperation.TimeUtc
    //                             ));
    //                     }
    //                 }
    //                 else if (addServiceAction is not null)
    //                 {
    //                     if (resolved.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(addServiceAction.ServiceId, StringComparison.InvariantCultureIgnoreCase)) is not null)
    //                     {
    //                         // should never happen
    //                         return Result.Fail("Fatal error. The service which should be added already exists");
    //                     }
    //                     else
    //                     {
    //                         prismServices.Add(new PrismService(
    //                             serviceId: addServiceAction.ServiceId,
    //                             type: addServiceAction.Type,
    //                             serviceEndpoints: addServiceAction.SerivceEndpoints.Split("||").ToList(),
    //                             addedOn: new PrismLedgerData(
    //                                 transactionId: PrismEncoding.ByteArrayToHex(updateOperation.TransactionHash),
    //                                 ledgerType: updateOperation.LedgerType,
    //                                 timestampInfo: new LedgerTimestampInfo(
    //                                     blockSequenceNumber: (uint)updateOperation.Index,
    //                                     operationSequenceNumber: (uint)updateOperation.OperationSequenceNumber,
    //                                     blockTimestamp: updateOperation.TimeUtc
    //                                 )),
    //                             deletedOn: null,
    //                             updatedOn: null));
    //                     }
    //                 }
    //                 else if (updateServiceAction is not null)
    //                 {
    //                     var serviceToUpdate = prismServices.FirstOrDefault(p => p.ServiceId.Equals(updateServiceAction.ServiceId, StringComparison.InvariantCultureIgnoreCase));
    //                     if (serviceToUpdate is null)
    //                     {
    //                         // should never happen
    //                         return Result.Fail("Fatal error. The service which should be updated does not exist");
    //                     }
    //                     else if (serviceToUpdate.DeletedOn is not null)
    //                     {
    //                         // should never happen. The key was already revoked. Revoking the key another time does not change its revocation-date;
    //                         return Result.Fail("Fatal error. The service was already removed and therefor cannot be updated");
    //                     }
    //
    //                     serviceToUpdate.Type = updateServiceAction.Type;
    //                     serviceToUpdate.ServiceEndpoints = updateServiceAction.SerivceEndpoints.Split("||").ToList();
    //                     serviceToUpdate.UpdatedOn = new PrismLedgerData(
    //                         transactionId: PrismEncoding.ByteArrayToHex(updateOperation.TransactionHash),
    //                         ledgerType: updateOperation.LedgerType,
    //                         timestampInfo: new LedgerTimestampInfo(
    //                             blockSequenceNumber: (uint)updateOperation.Index,
    //                             operationSequenceNumber: (uint)updateOperation.OperationSequenceNumber,
    //                             blockTimestamp: updateOperation.TimeUtc
    //                         ));
    //                 }
    //                 else if (removeServiceAction is not null)
    //                 {
    //                     var serviceToRemove = prismServices.FirstOrDefault(p => p.ServiceId.Equals(removeServiceAction.ServiceId, StringComparison.InvariantCultureIgnoreCase));
    //                     if (serviceToRemove is null)
    //                     {
    //                         // should never happen
    //                         return Result.Fail("Fatal error. The service which should be removed does not exist");
    //                     }
    //                     else if (serviceToRemove.DeletedOn is not null)
    //                     {
    //                         // should never happen. The key was already revoked. Revoking the key another time does not change its revocation-date;
    //                         return Result.Fail("Fatal error. The service was already removed");
    //                     }
    //
    //                     serviceToRemove.DeletedOn = new PrismLedgerData(
    //                         transactionId: PrismEncoding.ByteArrayToHex(updateOperation.TransactionHash),
    //                         ledgerType: updateOperation.LedgerType,
    //                         timestampInfo: new LedgerTimestampInfo(
    //                             blockSequenceNumber: (uint)updateOperation.Index,
    //                             operationSequenceNumber: (uint)updateOperation.OperationSequenceNumber,
    //                             blockTimestamp: updateOperation.TimeUtc
    //                         ));
    //                 }
    //                 else
    //                 {
    //                     throw new Exception("Operation index error in database");
    //                 }
    //             }
    //         }
    //
    //         resolved.PublicKeys.Clear();
    //         resolved.PublicKeys.AddRange(prismPublicKeys.OrderBy(p => p.KeyId));
    //         resolved.PrismServices.Clear();
    //         resolved.PrismServices.AddRange(prismServices.OrderBy(p => p.ServiceId));
    //     }
    //
    //     //lastly we have to apply the deactivateDid operation
    //     if (createDidResult.DeactivateDid is not null)
    //     {
    //         lastOperationHash = createDidResult.DeactivateDid.OperationHash;
    //         foreach (var publicKey in resolved.PublicKeys)
    //         {
    //             if (publicKey.RevokedOn is null)
    //             {
    //                 publicKey.RevokedOn = new PrismLedgerData(
    //                     transactionId: PrismEncoding.ByteArrayToHex(createDidResult.DeactivateDid.TransactionHash),
    //                     ledgerType: createDidResult.DeactivateDid.LedgerType,
    //                     timestampInfo: new LedgerTimestampInfo(
    //                         blockSequenceNumber: (uint)createDidResult.DeactivateDid.Index,
    //                         operationSequenceNumber: (uint)createDidResult.DeactivateDid.OperationSequenceNumber,
    //                         blockTimestamp: createDidResult.DeactivateDid.TimeUtc
    //                     ));
    //             }
    //         }
    //
    //         foreach (var service in resolved.PrismServices)
    //         {
    //             if (service.DeletedOn is null)
    //             {
    //                 service.DeletedOn = new PrismLedgerData(
    //                     transactionId: PrismEncoding.ByteArrayToHex(createDidResult.DeactivateDid.TransactionHash),
    //                     ledgerType: createDidResult.DeactivateDid.LedgerType,
    //                     timestampInfo: new LedgerTimestampInfo(
    //                         blockSequenceNumber: (uint)createDidResult.DeactivateDid.Index,
    //                         operationSequenceNumber: (uint)createDidResult.DeactivateDid.OperationSequenceNumber,
    //                         blockTimestamp: createDidResult.DeactivateDid.TimeUtc
    //                     ));
    //             }
    //         }
    //     }
    //
    //
    //     var resolveDidResponse = new ResolveDidResponse(resolved, Hash.CreateFrom(lastOperationHash));
    //
    //     return Result.Ok(resolveDidResponse);
    // }
    //
    // private static DidDocument ResolveCreateDidOperation(ResolveDidRequest request, CreateDidResult createDidResult)
    // {
    //     var returnDocument = new DidDocument(request.Did, publicKeys: new List<PrismPublicKey>(), prismServices: new List<PrismService>());
    //     var prismPublicKeys = new List<PrismPublicKey>();
    //     var prismServices = new List<PrismService>();
    //     var publicKeys = createDidResult.PublicKeys;
    //     foreach (var prismPublicKeyEntity in publicKeys)
    //     {
    //         var keyId = prismPublicKeyEntity.KeyId;
    //         var publicKeyLongFormBytes = prismPublicKeyEntity.PublicKey;
    //         var keysXy = PrismEncoding.HexToPublicKeyPairByteArrays(PrismEncoding.ByteArrayToHex(publicKeyLongFormBytes));
    //         var (keyX, keyY) = keysXy.Value;
    //         var keyUsage = prismPublicKeyEntity.PrismKeyUsage;
    //         prismPublicKeys.Add(new PrismPublicKey(
    //             keyUsage: keyUsage,
    //             keyId: keyId!,
    //             curve: prismPublicKeyEntity.Curve,
    //             keyX: keyX,
    //             keyY: keyY,
    //             addedOn: new PrismLedgerData(
    //                 transactionId: PrismEncoding.ByteArrayToHex(createDidResult.TransactionHash),
    //                 ledgerType: createDidResult.LedgerType,
    //                 timestampInfo: new LedgerTimestampInfo(
    //                     blockSequenceNumber: (uint)createDidResult.Index,
    //                     operationSequenceNumber: (uint)createDidResult.OperationSequenceNumber,
    //                     blockTimestamp: createDidResult.TimeUtc
    //                 )),
    //             revokedOn: null
    //         ));
    //     }
    //
    //     var services = createDidResult.Services;
    //     foreach (var service in services)
    //     {
    //         prismServices.Add(new PrismService(
    //             serviceId: service.ServiceId,
    //             type: service.Type,
    //             serviceEndpoints: service.SerivceEndpoints.Split("||").ToList(),
    //             addedOn: new PrismLedgerData(
    //                 transactionId: PrismEncoding.ByteArrayToHex(createDidResult.TransactionHash),
    //                 ledgerType: createDidResult.LedgerType,
    //                 timestampInfo: new LedgerTimestampInfo(
    //                     blockSequenceNumber: (uint)createDidResult.Index,
    //                     operationSequenceNumber: (uint)createDidResult.OperationSequenceNumber,
    //                     blockTimestamp: createDidResult.TimeUtc
    //                 )),
    //             deletedOn: null,
    //             updatedOn: null
    //         ));
    //     }
    //
    //     returnDocument.PublicKeys.AddRange(prismPublicKeys.OrderBy(p => p.KeyId));
    //     returnDocument.PrismServices.AddRange(prismServices);
    //     return returnDocument;

    return Result.Ok();
    }

    /// <summary>
    /// Method to order UpdateDid Operations in the correct order beginning from the past up to the present
    /// </summary>
    /// <param name="updateDidResults"></param>
    /// <returns></returns>
    // public static List<UpdateDidResult> OrderUpdateOperations(List<UpdateDidResult> updateDidResults)
    // {
    //     var finalOrderdList = new List<UpdateDidResult>();
    //     var groupedByBlockHeightList = updateDidResults.GroupBy(p => p.BlockHeight).OrderBy(p => p.Key).ToList();
    //     foreach (var groupedByBlockHeight in groupedByBlockHeightList)
    //     {
    //         var groupedByBlockSequenceList = groupedByBlockHeight.GroupBy(p => p.Index).OrderBy(p => p.Key).ToList();
    //         foreach (var groupedByBlockSequence in groupedByBlockSequenceList)
    //         {
    //             var groupedByOperationSequence = groupedByBlockSequence.OrderBy(p => p.OperationSequenceNumber).ToList();
    //             finalOrderdList.AddRange(groupedByOperationSequence);
    //         }
    //     }
    //
    //     return finalOrderdList;
    // }
}