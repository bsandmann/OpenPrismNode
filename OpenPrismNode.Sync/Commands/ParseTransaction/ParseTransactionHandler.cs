﻿namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using Core;
using Core.Commands.ResolveDid;
using Core.Common;
using Core.Crypto;
using Core.Models;
using Core.Services.Did;
using EnsureThat;
using FluentResults;
using Google.Protobuf;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Sync;
using OpenPrismNode.Sync.Models;

/// <summary>
/// Takes in the SignedAtalaOpertion and parses it into a OperationResultWrapper.
/// In this process all the signtatures get verified and the operation is checked for validity.
/// Parsing might fail, exspecially if it involves resolving a DID (required for UpdateDid and DeactivateDid operations),
/// which rely on a previous operation.
/// The parsing operation can be used either when reading data from chain, to check their validity and if the operation
/// should be added to the database of valid opertions, or when creating a new operation, to check if the operation is valid
/// and is aligned with previous operations.
/// </summary>
public class ParseTransactionHandler : IRequestHandler<ParseTransactionRequest, Result<OperationResultWrapper>>
{
    private readonly IMediator _mediator;
    private readonly ICryptoService _cryptoService;
    private readonly ISha256Service _sha256Service;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public ParseTransactionHandler(IMediator mediator, ISha256Service sha256Service, ICryptoService cryptoService, ILogger<ParseTransactionHandler> logger)
    {
        _mediator = mediator;
        _cryptoService = cryptoService;
        _sha256Service = sha256Service;
        _logger = logger;
    }

    public async Task<Result<OperationResultWrapper>> Handle(ParseTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.SignedAtalaOperation.Operation.CreateDid != null)
        {
            return ParseCreateDidOperation(request.SignedAtalaOperation, request.Index);
        }
        else if (request.SignedAtalaOperation.Operation.UpdateDid != null)
        {
            return await ParseUpdateDidOperation(request.SignedAtalaOperation, request.Ledger, request.Index, request.ResolveMode!);
        }
        else if (request.SignedAtalaOperation.Operation.ProtocolVersionUpdate != null)
        {
            return await ParseProtocolVersionUpdateOperation(request.SignedAtalaOperation, request.Ledger, request.Index, request.ResolveMode!);
        }
        else if (request.SignedAtalaOperation.Operation.DeactivateDid != null)
        {
            return await ParseDeactivateDidOperation(request.SignedAtalaOperation, request.Ledger, request.Index, request.ResolveMode!);
        }
        else
        {
            if (request.SignedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.None)
            {
                // Likely an old issuing operation
                return Result.Fail(ParserErrors.UnsupportedOperation);
            }

            return Result.Fail(ParserErrors.UnknownOperation);
        }
    }

    public Result<OperationResultWrapper> ParseCreateDidOperation(SignedAtalaOperation signedAtalaOperation, int index)
    {
        var didData = signedAtalaOperation.Operation.CreateDid.DidData;

        var publicKeyParseResult = ParsePublicKey(didData);
        if (publicKeyParseResult.IsFailed)
        {
            return publicKeyParseResult.ToResult();
        }

        var serviceParseResult = ParseService(didData);
        if (serviceParseResult.IsFailed)
        {
            return serviceParseResult.ToResult();
        }

        var contextParseResult = ParseContext(didData);

        var signature = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Signature);
        var signedWith = signedAtalaOperation.SignedWith;
        var encodedAtalaOperation = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString());
        var hashedAtalaOperation = new Hash(_sha256Service).Of(encodedAtalaOperation);

        // the did identifier is the hash of the intitial operation when creating the did
        var didIdentifier = PrismEncoding.ByteArrayToHex(hashedAtalaOperation.Value);

        // Only perform the signature verification if the operation is signed. Not possible for long-form DIDs
        if (signedAtalaOperation.Signature != ByteString.Empty)
        {
            // case sensitive
            var publicKeyMaster = publicKeyParseResult.Value.SingleOrDefault(p => p.KeyId.Equals(signedWith));
            if (publicKeyMaster is null || publicKeyMaster.KeyUsage != PrismKeyUsage.MasterKey)
            {
                return Result.Fail(ParserErrors.DataOfDidCreationCannotBeConfirmedDueToMissingKey);
            }

            var verificationResult = _cryptoService.VerifyDataSecp256k1(PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString()), signature, publicKeyMaster.LongByteArray);
            if (!verificationResult)
            {
                return Result.Fail(ParserErrors.UnableToVerifySignature + $" for keyId: {publicKeyMaster.KeyId} on DID-Creation for did {didIdentifier}");
            }
        }

        // TODO: Check if the DID already exists in the database. See spec. Do I still need to do that here?

        var didDocument = new InternalDidDocument(didIdentifier, publicKeyParseResult.Value, serviceParseResult.Value, contextParseResult.Value, DateTime.UtcNow, String.Empty, 0, 0, String.Empty);
        var operationResultWrapper = new OperationResultWrapper(OperationResultType.CreateDid, index, didDocument, signedWith);
        return Result.Ok(operationResultWrapper);
    }


    private async Task<Result<OperationResultWrapper>> ParseUpdateDidOperation(SignedAtalaOperation signedAtalaOperation, LedgerType ledger, int index, ResolveMode resolveMode)
    {
        var signature = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Signature);
        var signedWith = signedAtalaOperation.SignedWith;
        var didIdentifier = signedAtalaOperation.Operation.UpdateDid.Id;
        if (signedAtalaOperation.Operation.UpdateDid.PreviousOperationHash.IsEmpty)
        {
            return Result.Fail(ParserErrors.InvalidPreviousOperationHash);
        }

        var previousOperationHash = signedAtalaOperation.Operation.UpdateDid.PreviousOperationHash;
        var operationBytes = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString());
        var actions = signedAtalaOperation.Operation.UpdateDid.Actions;
        var updateActionResults = new List<UpdateDidActionResult>();

        var verificationResult = await ResolveAndVerifySignature(signedAtalaOperation, resolveMode, ledger, didIdentifier, signedWith, signature);
        if (verificationResult.IsFailed)
        {
            return verificationResult.ToResult();
        }

        ResolveDidResponse resolvedDid = verificationResult.Value;

        foreach (var action in actions)
        {
            if (action.ActionCase == UpdateDIDAction.ActionOneofCase.AddKey)
            {
                var publicKey = action.AddKey.Key;
                var parsedPublicKey = ParsePublicKeyInternal(publicKey);
                if (parsedPublicKey.IsSuccess)
                {
                    //Check if the operation already exists
                    // case sensitive
                    var existingKeyWithIdentialId = resolvedDid.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(parsedPublicKey.Value.KeyId));
                    if (existingKeyWithIdentialId is not null && !UpdateStackEvaluation.UpdateActionStackLastKeyActionWasRemoveKey(updateActionResults, parsedPublicKey.Value.KeyId))
                    {
                        return Result.Fail(ParserErrors.KeyAlreadyAdded + $": {existingKeyWithIdentialId.KeyId} for DID {resolvedDid.InternalDidDocument.DidIdentifier}");
                    }

                    // Adding the same key multiple times is not allowed
                    // Key-Fragments can be case-sensitive!
                    if (UpdateStackEvaluation.UpdateActionStackLastKeyActionWasAddKey(updateActionResults, parsedPublicKey.Value.KeyId))
                    {
                        return Result.Fail($"{ParserErrors.DuplicateKeyIds}: {parsedPublicKey.Value.KeyId}");
                    }

                    updateActionResults.Add(new UpdateDidActionResult(parsedPublicKey.Value));
                }
                else
                {
                    return parsedPublicKey.ToResult();
                }
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.RemoveKey)
            {
                var removedKeyId = action.RemoveKey.KeyId;

                // case sensitive
                var keyIsExisting = resolvedDid.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(removedKeyId));
                if (keyIsExisting is null && !UpdateStackEvaluation.UpdateActionStackLastKeyActionWasAddKey(updateActionResults, removedKeyId))
                {
                    return Result.Fail(ParserErrors.KeyToBeRemovedNotFound + $": {removedKeyId} for DID {resolvedDid.InternalDidDocument.DidIdentifier}");
                }

                List<PrismPublicKey> existingMasterKeys = resolvedDid.InternalDidDocument.PublicKeys.Where(p => p.KeyUsage == PrismKeyUsage.MasterKey).ToList();

                // Removing the same key multiple times is not allowed
                // Key-Fragments can be case-sensitive!
                if (UpdateStackEvaluation.UpdateActionStackLastKeyActionWasRemoveKey(updateActionResults, removedKeyId))
                {
                    return Result.Fail($"{ParserErrors.KeyAlreadyRemovedInPreviousAction}: {removedKeyId}");
                }

                // Calculation if a master-key can be removed
                var keysRemovedInPreviousOperations = updateActionResults.Where(p => p.UpdateDidActionType == UpdateDidActionType.RemoveKey).Select(p => p.RemovedKeyId!).ToList();
                var keysRemovedInPreviousAndThisOperation = keysRemovedInPreviousOperations.Append(removedKeyId).Distinct().ToList();
                var masterKeysAddedInPreviousOperations = updateActionResults.Where(p => p.UpdateDidActionType == UpdateDidActionType.AddKey && p.PrismPublicKey!.KeyUsage == PrismKeyUsage.MasterKey).Select(p => p.PrismPublicKey!.KeyId).Distinct().ToList();
                var keysRemovedInPreviousAndThisOperationWhichAreMasterKeys = keysRemovedInPreviousAndThisOperation.Where(p => existingMasterKeys.Any(q => q.KeyId.Equals(p)) || masterKeysAddedInPreviousOperations.Contains(p)).ToList();

                if (keysRemovedInPreviousAndThisOperationWhichAreMasterKeys.Count >= masterKeysAddedInPreviousOperations.Count + existingMasterKeys.Count)
                {
                    return Result.Fail(ParserErrors.UpdateOperationMasterKey);
                }

                updateActionResults.Add(new UpdateDidActionResult(removedKeyId));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.AddService)
            {
                // case sensitive!
                var existingServiceWithIdenticalId = resolvedDid.InternalDidDocument.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(action.AddService.Service.Id));
                if (existingServiceWithIdenticalId is not null && (UpdateStackEvaluation.UpdateActionStackLastServiceActionWasAddService(updateActionResults, action.AddService.Service.Id) ||
                                                                   UpdateStackEvaluation.UpdateActionStackLastServiceActionWasUpdateService(updateActionResults, action.AddService.Service.Id)) ||
                    (existingServiceWithIdenticalId is not null && !UpdateStackEvaluation.UpdateActionStackContainsServiceId(updateActionResults, action.AddService.Service.Id)))
                {
                    return Result.Fail(ParserErrors.ServiceAlreadyAdded + $": {existingServiceWithIdenticalId.ServiceId} for DID {resolvedDid.InternalDidDocument.DidIdentifier}");
                }

                if (UpdateStackEvaluation.UpdateActionStackLastServiceActionWasAddService(updateActionResults, action.AddService.Service.Id) || UpdateStackEvaluation.UpdateActionStackLastServiceActionWasUpdateService(updateActionResults, action.AddService.Service.Id))
                {
                    return Result.Fail(ParserErrors.ServiceAlreadyAdded + $": {action.AddService.Service.Id}");
                }

                var parsedService = ParseServiceInternal(action.AddService.Service.Type, action.AddService.Service.Id, action.AddService.Service.ServiceEndpoint, _logger);
                if (parsedService.IsFailed)
                {
                    return parsedService.ToResult();
                }

                updateActionResults.Add(new UpdateDidActionResult(parsedService.Value));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.RemoveService)
            {
                var removedServiceId = action.RemoveService.ServiceId;

                // case sensitive!
                var serviceIsExisting = resolvedDid.InternalDidDocument.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(removedServiceId));
                if (serviceIsExisting is null && UpdateStackEvaluation.UpdateActionStackLastServiceActionWasRemoveService(updateActionResults, removedServiceId) ||
                    (serviceIsExisting is null && !UpdateStackEvaluation.UpdateActionStackContainsServiceId(updateActionResults, removedServiceId)))
                {
                    return Result.Fail(ParserErrors.ServiceToBeRemovedNotFound + $": {removedServiceId} for DID {resolvedDid.InternalDidDocument.DidIdentifier}");
                }

                if (UpdateStackEvaluation.UpdateActionStackLastServiceActionWasRemoveService(updateActionResults, removedServiceId))
                {
                    return Result.Fail(ParserErrors.ServiceAlreadyRemoved + $": {action.RemoveService.ServiceId}");
                }

                updateActionResults.Add(new UpdateDidActionResult(action.RemoveService.ServiceId, true));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.UpdateService)
            {
                var updatedServiceId = action.UpdateService.ServiceId;

                // case sensitive!
                var serviceIsExisting = resolvedDid.InternalDidDocument.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(updatedServiceId));
                if (serviceIsExisting is null && UpdateStackEvaluation.UpdateActionStackLastServiceActionWasRemoveService(updateActionResults, updatedServiceId))
                {
                    return Result.Fail(ParserErrors.ServiceToBeUpdatedNotFound + $": {updatedServiceId} for DID {resolvedDid.InternalDidDocument.DidIdentifier}");
                }

                if (UpdateStackEvaluation.UpdateActionStackLastServiceActionWasRemoveService(updateActionResults, updatedServiceId))
                {
                    return Result.Fail(ParserErrors.ServiceAlreadyRemoved + $": {action.RemoveService.ServiceId}");
                }

                var parsedService = ParseServiceInternal(action.UpdateService.Type, action.UpdateService.ServiceId, action.UpdateService.ServiceEndpoints, _logger);
                if (parsedService.IsFailed)
                {
                    return parsedService.ToResult();
                }

                updateActionResults.Add(new UpdateDidActionResult(parsedService.Value.ServiceId, parsedService.Value.Type, parsedService.Value.ServiceEndpoints));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.PatchContext)
            {
                // TODO verify against on-chain data if available: See on master

                if (action.PatchContext.Context.Any())
                {
                    // Replace all
                    updateActionResults.Add(new UpdateDidActionResult(action.PatchContext.Context.ToList()));
                }
                else
                {
                    // Remove all exisisting
                    updateActionResults.Add(new UpdateDidActionResult(new List<string>()));
                }
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.None)
            {
                return Result.Fail("Invalid action construction");
            }
        }

        // Postoperation validation. See SPEC:
        // After all operations finshied, there must be at least one master key
        if (UpdateStackEvaluation.GetNumberOfMasterKeys(updateActionResults, resolvedDid.InternalDidDocument.PublicKeys.Where(p => p.KeyUsage == PrismKeyUsage.MasterKey).Select(p => p.KeyId).ToList()) < 1)
        {
            return Result.Fail(ParserErrors.NoMasterKey);
        }

        // Service number must not exceed the maximum allowed number of services according to the global PrismParameters
        if (UpdateStackEvaluation.GetNumberOfServices(updateActionResults, resolvedDid.InternalDidDocument.PrismServices.Select(p => p.ServiceId).ToList()) > PrismParameters.MaxServiceNumber)
        {
            return Result.Fail(ParserErrors.MaxServiceNumber);
        }

        // VerificationKeys number must not exceed the maximum allowed number of verification methods according to the global PrismParameters
        if (UpdateStackEvaluation.GetNumberOfVerificationMethods(updateActionResults, resolvedDid.InternalDidDocument.PublicKeys.Select(p => p.KeyId).ToList()) > PrismParameters.MaxVerifiactionMethodNumber)
        {
            return Result.Fail(ParserErrors.MaxVerifiactionMethodNumber);
        }

        var operationResultWrapper = new OperationResultWrapper(OperationResultType.UpdateDid, index, didIdentifier, Hash.CreateFrom(PrismEncoding.ByteStringToByteArray(previousOperationHash)), updateActionResults, operationBytes, signature, signedWith);
        return Result.Ok(operationResultWrapper);
    }

    private async Task<Result<OperationResultWrapper>> ParseProtocolVersionUpdateOperation(SignedAtalaOperation signedAtalaOperation, LedgerType ledger, int index, ResolveMode resolveMode)
    {
        // TODO verify against on-chain data if available: See on master

        var update = signedAtalaOperation.Operation.ProtocolVersionUpdate;
        var signature = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Signature);
        var signedWith = signedAtalaOperation.SignedWith;
        var version = update.Version;
        if (version is null)
        {
            return Result.Fail(ParserErrors.InvalidVersionInProtocolUpdate);
        }

        var proposerDidIdentifier = update.ProposerDid;

        var verificationResult = await ResolveAndVerifySignature(signedAtalaOperation, resolveMode, ledger, proposerDidIdentifier, signedWith, signature);
        if (verificationResult.IsFailed)
        {
            return verificationResult.ToResult();
        }

        //TODO fetch the current version in the databse

        var previousVersionDummy = new ProtocolVersion(1, 0);

        if (!(previousVersionDummy.MajorVersion < version.ProtocolVersion.MajorVersion ||
              (previousVersionDummy.MajorVersion == version.ProtocolVersion.MajorVersion && previousVersionDummy.MinorVersion < version.ProtocolVersion.MinorVersion)))
        {
            return Result.Fail(ParserErrors.InvalidProtocolVersionUpdate + ": The new version must be higher than the current version.");
        }

        if (version.EffectiveSince <= 0)
        {
            return Result.Fail(ParserErrors.InvalidProtocolVersionUpdate + ": The effectiveSince block must be greater than 0.");
        }

        var prismProtocolVersion = new ProtocolVersionUpdate(
            effectiveSinceBlock: version.EffectiveSince,
            prismProtocolVersion: version.ProtocolVersion is null ? null : new ProtocolVersion(majorVersion: version.ProtocolVersion.MajorVersion, minorVersion: version.ProtocolVersion.MinorVersion),
            versionName: version.VersionName,
            proposerDidIdentifier: proposerDidIdentifier
        );

        var operationBytes = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString());
        var operationResultWrapper = new OperationResultWrapper(OperationResultType.ProtocolVersionUpdate, index, prismProtocolVersion, proposerDidIdentifier, operationBytes, signature, signedWith);
        return Result.Ok(operationResultWrapper);
    }

    private async Task<Result<OperationResultWrapper>> ParseDeactivateDidOperation(SignedAtalaOperation signedAtalaOperation, LedgerType ledger, int index, ResolveMode resolveMode)
    {
        var signature = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Signature);
        var signedWith = signedAtalaOperation.SignedWith;
        var didIdentifier = signedAtalaOperation.Operation.DeactivateDid.Id;
        var previousOperationHash = signedAtalaOperation.Operation.DeactivateDid.PreviousOperationHash;
        if (signedAtalaOperation.Operation.DeactivateDid.PreviousOperationHash.IsEmpty)
        {
            return Result.Fail(ParserErrors.InvalidPreviousOperationHash);
        }

        var operationBytes = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString());

        var verificationResult = await ResolveAndVerifySignature(signedAtalaOperation, resolveMode, ledger, didIdentifier, signedWith, signature);
        if (verificationResult.IsFailed)
        {
            return verificationResult.ToResult();
        }

        var operationResultWrapper = new OperationResultWrapper(OperationResultType.DeactivateDid, index, didIdentifier, Hash.CreateFrom(PrismEncoding.ByteStringToByteArray(previousOperationHash)), operationBytes, signature, signedWith);
        return Result.Ok(operationResultWrapper);
    }

    private static Result<List<PrismPublicKey>> ParsePublicKey(CreateDIDOperation.Types.DIDCreationData didData)
    {
        var publicKeys = new List<PrismPublicKey>();
        if (!didData.PublicKeys.Any())
        {
            return Result.Fail(ParserErrors.NoPublicKeyFound);
        }

        if (didData.PublicKeys.Count > PrismParameters.MaxVerifiactionMethodNumber)
        {
            return Result.Fail(ParserErrors.MaxVerifiactionMethodNumber);
        }

        if (didData.PublicKeys.All(p => p.Usage != KeyUsage.MasterKey))
        {
            return Result.Fail(ParserErrors.NoMasterKey);
        }

        foreach (var publicKey in didData.PublicKeys)
        {
            var parsedPublicKey = ParsePublicKeyInternal(publicKey);
            if (parsedPublicKey.IsFailed)
            {
                return parsedPublicKey.ToResult();
            }

            publicKeys.Add(parsedPublicKey.Value);
        }

        if (publicKeys.Select(p => p.KeyId).Distinct().Count() != publicKeys.Count)
        {
            return Result.Fail(ParserErrors.DuplicateKeyIds);
        }

        if (publicKeys.Any(p => p.KeyUsage == PrismKeyUsage.MasterKey && p.Curve != PrismParameters.Secp256k1CurveName))
        {
            return Result.Fail(ParserErrors.MasterKeyMustBeSecp256k1);
        }

        return Result.Ok(publicKeys);
    }

    private static Result<PrismPublicKey> ParsePublicKeyInternal(PublicKey publicKey)
    {
        string curve;
        byte[] keyData;

        if (publicKey.KeyDataCase == PublicKey.KeyDataOneofCase.EcKeyData)
        {
            curve = publicKey.EcKeyData.Curve;
            if (curve == PrismParameters.Secp256k1CurveName)
            {
                return ParseSecp256k1Key(publicKey);
            }
            else if (curve == PrismParameters.Ed25519CurveName || curve == PrismParameters.X25519CurveName)
            {
                if (publicKey.EcKeyData.X.IsEmpty)
                {
                    return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
                }

                keyData = publicKey.EcKeyData.X.ToByteArray();
            }
            else
            {
                return Result.Fail($"Unsupported curve: {curve}");
            }
        }
        else if (publicKey.KeyDataCase == PublicKey.KeyDataOneofCase.CompressedEcKeyData)
        {
            curve = publicKey.CompressedEcKeyData.Curve;
            if (curve == PrismParameters.Secp256k1CurveName)
            {
                return ParseCompressedSecp256k1Key(publicKey);
            }
            else if (curve == PrismParameters.Ed25519CurveName || curve == PrismParameters.X25519CurveName)
            {
                keyData = publicKey.CompressedEcKeyData.Data.ToByteArray();
            }
            else
            {
                return Result.Fail($"Unsupported curve: {curve}");
            }
        }
        else
        {
            return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
        }

        // Validate key data for ED25519 and X25519
        if (keyData.Length != 32)
        {
            return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
        }

        if (publicKey.Id.Length > PrismParameters.MaxIdSize)
        {
            return Result.Fail(ParserErrors.MaximumKeyIdSize);
        }

        if (publicKey.Usage == KeyUsage.UnknownKey)
        {
            return Result.Fail("The UnknownKey is not a valid key usage.");
        }

        if (curve == PrismParameters.Secp256k1CurveName)
        {
            return Result.Ok(new PrismPublicKey(
                keyUsage: Enum.Parse<PrismKeyUsage>(publicKey.Usage.ToString()),
                keyId: publicKey.Id,
                x: keyData,
                y: null,
                curve: curve
            ));
        }

        return Result.Ok(new PrismPublicKey(
            keyUsage: Enum.Parse<PrismKeyUsage>(publicKey.Usage.ToString()),
            keyId: publicKey.Id,
            curve: curve,
            rawBytes: keyData
        ));
    }

    private static Result<PrismPublicKey> ParseSecp256k1Key(PublicKey publicKey)
    {
        if (publicKey.EcKeyData.X.IsEmpty || publicKey.EcKeyData.Y.IsEmpty)
        {
            return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
        }

        var x = publicKey.EcKeyData.X.ToByteArray();
        var y = publicKey.EcKeyData.Y.ToByteArray();

        if (x.Length != 32 || y.Length != 32)
        {
            return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
        }

        return CreatePrismPublicKey(publicKey, x, y, PrismParameters.Secp256k1CurveName);
    }

    private static Result<PrismPublicKey> ParseCompressedSecp256k1Key(PublicKey publicKey)
    {
        var decompressedResult = PrismPublicKey.Decompress(publicKey.CompressedEcKeyData.Data.ToByteArray(), PrismParameters.Secp256k1CurveName);
        if (decompressedResult.IsFailed)
        {
            return decompressedResult.ToResult();
        }

        return CreatePrismPublicKey(publicKey, decompressedResult.Value.Item1, decompressedResult.Value.Item2, PrismParameters.Secp256k1CurveName);
    }

    private static Result<PrismPublicKey> CreatePrismPublicKey(PublicKey publicKey, byte[] x, byte[] y, string curve)
    {
        if (publicKey.Id.Length > PrismParameters.MaxIdSize)
        {
            return Result.Fail(ParserErrors.MaximumKeyIdSize);
        }

        if (publicKey.Usage == KeyUsage.UnknownKey)
        {
            return Result.Fail("The UnknownKey is not a valid key usage.");
        }

        return Result.Ok(new PrismPublicKey(
            keyUsage: Enum.Parse<PrismKeyUsage>(publicKey.Usage.ToString()),
            keyId: publicKey.Id,
            x: x,
            y: y,
            curve: curve
        ));
    }

    private Result<List<string>> ParseContext(CreateDIDOperation.Types.DIDCreationData didData)
    {
        var contexts = new List<string>();

        if (!didData.Context.Any())
        {
            return contexts;
        }

        return didData.Context.ToList();
    }

    private Result<List<PrismService>> ParseService(CreateDIDOperation.Types.DIDCreationData didData)
    {
        var services = new List<PrismService>();

        if (!didData.Services.Any())
        {
            return services;
        }

        if (didData.Services.Count > PrismParameters.MaxServiceNumber)
        {
            return Result.Fail(ParserErrors.MaxServiceNumber);
        }

        foreach (var service in didData.Services)
        {
            var serviceParseResult = ParseServiceInternal(service.Type, service.Id, service.ServiceEndpoint, _logger);
            if (serviceParseResult.IsFailed)
            {
                return serviceParseResult.ToResult();
            }

            services.Add(serviceParseResult.Value);
        }

        if (services.Select(p => p.ServiceId).Distinct().Count() != services.Count)
        {
            return Result.Fail(ParserErrors.DuplicateServiceIds);
        }

        return services;
    }

    private static Result<PrismService> ParseServiceInternal(string type, string id, string serviceEndpoint, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(type) || !type.Trim().Equals(type) || type.Length > PrismParameters.MaxTypeSize)
        {
            return Result.Fail($"{ParserErrors.ServiceTypeInvalid}: {type}");
        }

        if (type.StartsWith("[") && type.EndsWith("]"))
        {
            var listTypes = type.Substring(1, type.Length - 2).Split(",");
            foreach (var listType in listTypes)
            {
                if (!PrismParameters.ExpectedServiceTypes.Contains(listType.Replace("\"", string.Empty)))
                {
                    logger.LogWarning($"{ParserErrors.UnexpectedServiceType}: {type}");
                }
            }
        }
        else
        {
            if (!PrismParameters.ExpectedServiceTypes.Contains(type))
            {
                logger.LogWarning($"{ParserErrors.UnexpectedServiceType}: {type}");
            }
        }

        if (string.IsNullOrWhiteSpace(id) || id.Length > PrismParameters.MaxIdSize)
        {
            return Result.Fail(ParserErrors.InvalidServiceId);
        }

        if (serviceEndpoint.Length > PrismParameters.MaxServiceEndpointSize)
        {
            return Result.Fail($"{ParserErrors.ServiceEndpointInvalid}: {id}");
        }

        var parsedServiceEndpoint = ServiceEndpoints.Parse(serviceEndpoint);
        if (parsedServiceEndpoint.IsFailed)
        {
            return parsedServiceEndpoint.ToResult();
        }

        return Result.Ok(new PrismService(
            serviceId: id,
            type: type,
            serviceEndpoints: parsedServiceEndpoint.Value
        ));
    }

    private async Task<Result<ResolveDidResponse>> ResolveAndVerifySignature(SignedAtalaOperation signedAtalaOperation, ResolveMode resolveMode, LedgerType ledger, string didIdentifier, string signedWith, byte[] signature)
    {
        var resolved = await _mediator.Send(new ResolveDidRequest(ledger, didIdentifier, resolveMode.BlockHeight, resolveMode.BlockSequence, resolveMode.OperationSequence));
        if (resolved.IsFailed)
        {
            return Result.Fail(ParserErrors.UnableToResolveForPublicKeys + $" for {GetOperationResultType.GetFromSignedAtalaOperation(signedAtalaOperation)} operation for DID {didIdentifier}");
        }

        if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.UpdateDid || signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.DeactivateDid)
        {
            var previousOperationHashByteArray = signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.UpdateDid ? signedAtalaOperation.Operation.UpdateDid.PreviousOperationHash : signedAtalaOperation.Operation.DeactivateDid.PreviousOperationHash;
            var previousOperationHash = Hash.CreateFrom(PrismEncoding.ByteStringToByteArray(previousOperationHashByteArray));
            if (!previousOperationHash.Value.SequenceEqual(resolved.Value.LastOperationHash.Value))
            {
                return Result.Fail(ParserErrors.InvalidPreviousOperationHash + $" for {GetOperationResultType.GetFromSignedAtalaOperation(signedAtalaOperation)} operation for DID {didIdentifier}");
            }
            else if (previousOperationHashByteArray.SequenceEqual(PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString())))
            {
                return Result.Fail(ParserErrors.OperationAlreadyOnChain + $" for {GetOperationResultType.GetFromSignedAtalaOperation(signedAtalaOperation)} operation for DID {didIdentifier}");
            }
        }

        PrismPublicKey? publicKey;
        if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.CreateDid ||
            signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.UpdateDid ||
            signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.DeactivateDid)
        {
            publicKey = resolved.Value.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(signedWith) && p.KeyUsage == PrismKeyUsage.MasterKey);
        }
        else if (signedAtalaOperation.Operation.OperationCase == AtalaOperation.OperationOneofCase.ProtocolVersionUpdate)
        {
            // TODO Unclear . Read spec
            publicKey = resolved.Value.InternalDidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(signedWith, StringComparison.InvariantCultureIgnoreCase));
        }
        else
        {
            throw new NotImplementedException();
        }

        if (publicKey is null)
        {
            return Result.Fail(ParserErrors.UnableToResolveForPublicKeys + $" for {GetOperationResultType.GetFromSignedAtalaOperation(signedAtalaOperation)} operation for DID {didIdentifier}");
        }

        var verificationResult = _cryptoService.VerifyDataSecp256k1(PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString()), signature, publicKey.LongByteArray);
        if (!verificationResult)
        {
            return Result.Fail(ParserErrors.UnableToVerifySignature + $" for {GetOperationResultType.GetFromSignedAtalaOperation(signedAtalaOperation)} operation for DID {didIdentifier}");
        }

        return Result.Ok(resolved.Value);
    }
}