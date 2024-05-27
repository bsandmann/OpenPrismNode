namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using System.Diagnostics;
using Core;
using Core.Crypto;
using Core.Models;
using EnsureThat;
using FluentResults;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenPrismNode.Sync;
using OpenPrismNode.Sync.Models;

/// <summary>
/// Parses a json string and outputs a object describing the parsed operation
/// Parsing might fail, exspecially if it involves resolving the did and checking the validity of the operation
/// </summary>
public class ParseTransactionHandler : IRequestHandler<ParseTransactionRequest, Result<OperationResultWrapper>>
{
    private readonly IMediator _mediator;
    private readonly IEcService _ecService;
    private readonly ISha256Service _sha256Service;
    private readonly ILogger<ParseTransactionHandler> _logger;

    public ParseTransactionHandler(IMediator mediator, ISha256Service sha256Service, IEcService ecService, ILogger<ParseTransactionHandler> logger)
    {
        _mediator = mediator;
        _ecService = ecService;
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
            return await ParseUpdateDidOperation(request.SignedAtalaOperation, request.Index, request.ResolveMode);
        }
        else if (request.SignedAtalaOperation.Operation.ProtocolVersionUpdate != null)
        {
            return await ParseProtocolVersionUpdateOperation(request.SignedAtalaOperation, request.Index, request.ResolveMode);
        }
        else if (request.SignedAtalaOperation.Operation.DeactivateDid != null)
        {
            return await ParseDeactivateDidOperation(request.SignedAtalaOperation, request.Index, request.ResolveMode);
        }
        else
        {
            return Result.Fail(ParserErrors.UnknownOperation);
        }
    }

    private Result<OperationResultWrapper> ParseCreateDidOperation(SignedAtalaOperation signedAtalaOperation, int index)
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

        var contexts = didData.Context.ToList();
        if (!contexts.Any())
        {
            _logger.LogWarning("No context found in the createDid operation. At least one context should be required");
            // return Result.Fail("No context found in the createDid operation. At least one context is required.");
        }
        else
        {
            var wow = true;
        }

        var signature = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Signature);
        var signedWith = signedAtalaOperation.SignedWith;
        var publicKeyMaster = publicKeyParseResult.Value.SingleOrDefault(p => p.KeyId.Equals(signedWith, StringComparison.InvariantCultureIgnoreCase));
        if (publicKeyMaster is null || publicKeyMaster.KeyUsage != PrismKeyUsage.MasterKey)
        {
            return Result.Fail(ParserErrors.DataOfDidCreationCannotBeConfirmedDueToMissingKey);
        }

        var encodedAtalaOperation = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString());
        var hashedAtalaOperation = new Hash(_sha256Service).Of(encodedAtalaOperation);

        // the did identifier is the hash of the intitial operation when creating the did
        var didIdentifier = PrismEncoding.ByteArrayToHex(hashedAtalaOperation.Value);

        var verificationResult = _ecService.VerifyData(PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString()), signature, publicKeyMaster.LongByteArray);
        if (!verificationResult)
        {
            return Result.Fail(ParserErrors.UnableToVerifySignature + $" for keyId: {publicKeyMaster.KeyId} on DID-Creation for did {didIdentifier}");
        }

        var didDocument = new DidDocument(didIdentifier, publicKeyParseResult.Value, serviceParseResult.Value, contexts);
        var operationResultWrapper = new OperationResultWrapper(OperationResultType.CreateDid, index, didDocument, signedWith);
        return Result.Ok(operationResultWrapper);
    }


    private async Task<Result<OperationResultWrapper>> ParseUpdateDidOperation(SignedAtalaOperation signedAtalaOperation, int index, ResolveMode resolveMode)
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

        ResolveDidResponse resolvedDid = null!;
        // OUT OF SCOPE
        // if (resolveMode.ParserResolveMode == ParserResolveMode.ResolveAgainstDatabaseAndVerifySignature)
        // {
        //    //TODO see removed Codebase -> Refactor
        //     var verificationResult = await ResolveAndVerifySignature(signedAtalaOperation, resolveMode, didIdentifier, signedWith, signature);
        //     if (verificationResult.IsFailed)
        //     {
        //         return verificationResult.ToResult();
        //     }
        //
        //     resolvedDid = verificationResult.Value;
        // }

        // This sorting causes the addKeys to come first - which is relevant for keyRotations, where the ordering is irrelevant. That means, before removing the last masterKey we must be sure, that a new one was added
        // TODO Unclear, since the SPEC defines that the actions have to performed in order
        foreach (var action in actions.OrderBy(p => p.ActionCase == UpdateDIDAction.ActionOneofCase.RemoveKey))
        {
            if (action.ActionCase == UpdateDIDAction.ActionOneofCase.AddKey)
            {
                var publicKey = action.AddKey.Key;
                var parsedPublicKey = ParsePublicKeyInternal(publicKey);
                if (parsedPublicKey.IsSuccess)
                {
                    //Check if the operation already exists
                    if (resolvedDid is not null)
                    {
                        var existingKeyWithIdentialId = resolvedDid.DidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(parsedPublicKey.Value.KeyId, StringComparison.InvariantCultureIgnoreCase));
                        if (existingKeyWithIdentialId is not null)
                        {
                            return Result.Fail(ParserErrors.KeyAlreadyAdded + $": {existingKeyWithIdentialId.KeyId} for DID {resolvedDid.DidDocument.DidIdentifier}");
                        }
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

                if (resolvedDid is not null)
                {
                    var keyIsExisting = resolvedDid.DidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(removedKeyId, StringComparison.InvariantCultureIgnoreCase));
                    if (keyIsExisting is null)
                    {
                        return Result.Fail(ParserErrors.KeyToBeRemovedNotFound + $": {removedKeyId} for DID {resolvedDid.DidDocument.DidIdentifier}");
                    }

                    // var alreadyRemovedKey = resolvedDid.DidDocument.PublicKeys.FirstOrDefault(p => p.KeyId.Equals(removedKeyId, StringComparison.InvariantCultureIgnoreCase) && p.RevokedOn is not null);
                    // if (alreadyRemovedKey is not null)
                    // {
                    //     return Result.Fail(ParserErrors.KeyAlreadyRevoked + $": {removedKeyId} for DID {resolvedDid.DidDocument.Did.Identifier}");
                    // }

                    var keyToRemove = resolvedDid.DidDocument.PublicKeys.Single(p => p.KeyId.Equals(removedKeyId, StringComparison.InvariantCultureIgnoreCase));
                    // var hasOtherExistingMasterKeys = resolvedDid.DidDocument.PublicKeys.Any(p => p.KeyUsage == PrismKeyUsage.MasterKey && p.RevokedOn is null && !p.KeyId.Equals(removedKeyId, StringComparison.InvariantCultureIgnoreCase));
                    // var hasOtherNewlyAddedMasterKeys = updateActionResults.Any(p => p.UpdateDidActionType == UpdateDidActionType.AddKey && p.PrismPublicKey!.KeyUsage == PrismKeyUsage.MasterKey && !p.PrismPublicKey.KeyId.Equals(removedKeyId, StringComparison.InvariantCultureIgnoreCase));
                    // if (keyToRemove.KeyUsage == PrismKeyUsage.MasterKey && !hasOtherExistingMasterKeys && !hasOtherNewlyAddedMasterKeys)
                    // {
                    //     return Result.Fail(ParserErrors.LastMasterKeyCannotBeRevoked + $": {removedKeyId} for DID {resolvedDid.DidDocument.Did.Identifier}");
                    // }
                }

                updateActionResults.Add(new UpdateDidActionResult(removedKeyId));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.AddService)
            {
                if (resolvedDid is not null)
                {
                    var existingServiceWithIdenticalId = resolvedDid.DidDocument.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(action.AddService.Service.Id, StringComparison.InvariantCultureIgnoreCase));
                    if (existingServiceWithIdenticalId is not null)
                    {
                        return Result.Fail(ParserErrors.ServiceAlreadyAdded + $": {existingServiceWithIdenticalId.ServiceId} for DID {resolvedDid.DidDocument.DidIdentifier}");
                    }

                    var parsedService = ParseServiceInternal(action.AddService.Service, _logger);
                    if (parsedService.IsFailed)
                    {
                        return parsedService.ToResult();
                    }

                    updateActionResults.Add(new UpdateDidActionResult(parsedService.Value));
                }
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.RemoveService)
            {
                var removedServiceId = action.RemoveService.ServiceId;

                if (resolvedDid is not null)
                {
                    var serviceIsExisting = resolvedDid.DidDocument.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(removedServiceId, StringComparison.InvariantCultureIgnoreCase));
                    if (serviceIsExisting is null)
                    {
                        return Result.Fail(ParserErrors.ServiceToBeRemovedNotFound + $": {removedServiceId} for DID {resolvedDid.DidDocument.DidIdentifier}");
                    }
                }

                updateActionResults.Add(new UpdateDidActionResult(action.RemoveService.ServiceId, true));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.UpdateService)
            {
                var updatedServiceId = action.UpdateService.ServiceId;

                if (resolvedDid is not null)
                {
                    var serviceIsExisting = resolvedDid.DidDocument.PrismServices.FirstOrDefault(p => p.ServiceId.Equals(updatedServiceId, StringComparison.InvariantCultureIgnoreCase));
                    if (serviceIsExisting is null)
                    {
                        return Result.Fail(ParserErrors.ServiceToBeUpdatedNotFound + $": {updatedServiceId} for DID {resolvedDid.DidDocument.DidIdentifier}");
                    }

                    var parsedService = ParseServiceInternal(action.AddService.Service, _logger);
                    if (parsedService.IsFailed)
                    {
                        return parsedService.ToResult();
                    }
                }

                // TODO Verify according to spec

                // TODO Implementa correct UpdateType
                updateActionResults.Add(new UpdateDidActionResult(action.UpdateService.ServiceId, action.UpdateService.Type, null));
            }
            else if (action.ActionCase == UpdateDIDAction.ActionOneofCase.PatchContext)
            {
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

        // TODO Postoperation validation

        var operationResultWrapper = new OperationResultWrapper(OperationResultType.UpdateDid, index, didIdentifier, Hash.CreateFrom(PrismEncoding.ByteStringToByteArray(previousOperationHash)), updateActionResults, operationBytes, signature, signedWith);
        return Result.Ok(operationResultWrapper);
    }

    private async Task<Result<OperationResultWrapper>> ParseProtocolVersionUpdateOperation(SignedAtalaOperation signedAtalaOperation, int index, ResolveMode resolveMode)
    {
        var update = signedAtalaOperation.Operation.ProtocolVersionUpdate;
        var signature = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Signature);
        var signedWith = signedAtalaOperation.SignedWith;
        var version = update.Version;
        if (version is null)
        {
            return Result.Fail(ParserErrors.InvalidVersionInProtocolUpdate);
        }

        var proposerDidIdentifier = update.ProposerDid;

        if (resolveMode.ParserResolveMode == ParserResolveMode.ResolveAgainstDatabaseAndVerifySignature)
        {
            // OUT OF SCOPE
            // var verificationResult = await ResolveAndVerifySignature(signedAtalaOperation, resolveMode, proposerDid, signedWith, signature);
            // if (verificationResult.IsFailed)
            // {
            //     return verificationResult.ToResult();
            // }
        }

        //TODO fetch the current version in the databse
        
        var previousVersionDummy = new PrismProtocolVersion(1, 0);

        if (!(previousVersionDummy.MajorVersion < version.ProtocolVersion.MajorVersion ||
              (previousVersionDummy.MajorVersion == version.ProtocolVersion.MajorVersion && previousVersionDummy.MinorVersion < version.ProtocolVersion.MinorVersion)))
        {
            return Result.Fail(ParserErrors.InvalidProtocolVersionUpdate + ": The new version must be higher than the current version.");
        }
        
        if (version.EffectiveSince <= 0)
        {
            return Result.Fail(ParserErrors.InvalidProtocolVersionUpdate + ": The effectiveSince block must be greater than 0.");
        }

        var prismProtocolVersion = new PrismProtocolVersionUpdate(
            effectiveSinceBlock: version.EffectiveSince,
            prismProtocolVersion: version.ProtocolVersion is null ? null : new PrismProtocolVersion(majorVersion: version.ProtocolVersion.MajorVersion, minorVersion: version.ProtocolVersion.MinorVersion),
            versionName: version.VersionName,
            proposerDidIdentifier: proposerDidIdentifier
        );

        var operationBytes = PrismEncoding.ByteStringToByteArray(signedAtalaOperation.Operation.ToByteString());
        var operationResultWrapper = new OperationResultWrapper(OperationResultType.ProtocolVersionUpdate, index, prismProtocolVersion, proposerDidIdentifier, operationBytes, signature, signedWith);
        return Result.Ok(operationResultWrapper);
    }

    private async Task<Result<OperationResultWrapper>> ParseDeactivateDidOperation(SignedAtalaOperation signedAtalaOperation, int index, ResolveMode resolveMode)
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

        if (resolveMode.ParserResolveMode == ParserResolveMode.ResolveAgainstDatabaseAndVerifySignature)
        {
            // OUT OF SCOPE
            // var verificationResult = await ResolveAndVerifySignature(signedAtalaOperation, resolveMode, didIdentifier, signedWith, signature);
            // if (verificationResult.IsFailed)
            // {
            //     return verificationResult.ToResult();
            // }
        }

        var operationResultWrapper = new OperationResultWrapper(OperationResultType.DeactivateDid, index, didIdentifier, Hash.CreateFrom(PrismEncoding.ByteStringToByteArray(previousOperationHash)), operationBytes, signature, signedWith);
        return Result.Ok(operationResultWrapper);
    }

    private static Result<List<PrismPublicKey>> ParsePublicKey(CreateDIDOperation.Types.DIDCreationData didData)
    {
        var publicKeys = new List<PrismPublicKey>();
        if (!didData.PublicKeys.Any())
        {
            return Result.Fail("No public keys found in the createDid operation. At least one public key is required.");
        }

        if (didData.PublicKeys.Count > PrismParameters.MaxVerifiactionMethodNumber)
        {
            return Result.Fail("Public key number exceeds the maximum allowed number of verification methods according to the global PrismParameters.");
        }

        if (didData.PublicKeys.All(p => p.Usage != KeyUsage.MasterKey))
        {
            return Result.Fail("No master key found in the createDid operation. At least one master key is required.");
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
            return Result.Fail("Duplicate key IDs detected. Each key ID must be unique.");
        }

        return Result.Ok(publicKeys);
    }

    private static Result<PrismPublicKey> ParsePublicKeyInternal(PublicKey publicKey)
    {
        // TODO suuport Ed25519 and X25519
        if (publicKey.KeyDataCase == PublicKey.KeyDataOneofCase.EcKeyData)
        {
            Ensure.That(publicKey.EcKeyData.Curve.Equals(PrismParameters.Secp256k1CurveName));
            // Debug.Assert(publicKey.EcKeyData.Curve.Equals(PrismParameters.Ed25519CurveName));
        }

        else
        {
            Ensure.That(publicKey.CompressedEcKeyData.Curve.Equals(PrismParameters.Secp256k1CurveName));
            // Debug.Assert(publicKey.CompressedEcKeyData.Curve.Equals(PrismParameters.Ed25519CurveName));
        }

        byte[] x;
        byte[] y;
        if (publicKey.CompressedEcKeyData is not null)
        {
            var decompressedResult = PrismPublicKey.Decompress(publicKey.CompressedEcKeyData.Data.ToByteArray(), publicKey.CompressedEcKeyData.Curve);
            if (decompressedResult.IsFailed)
            {
                return decompressedResult.ToResult();
            }

            x = decompressedResult.Value.Item1;
            y = decompressedResult.Value.Item2;
        }
        else if (!publicKey.EcKeyData.X.IsEmpty && !publicKey.EcKeyData.Y.IsEmpty)
        {
            x = PrismEncoding.ByteStringToByteArray(publicKey.EcKeyData.X);
            y = PrismEncoding.ByteStringToByteArray(publicKey.EcKeyData.Y);
            if (x.Length != 32 || y.Length != 32)
            {
                // In the beginning of PRISM there are a lot of invalid publicKeys with length of 31, 32 and 33 bytes
                // I theoretically could reconstruct some of those keys, but I'll flag them as invalid to make it simpler and
                // not be compatible into the early days of prism
                return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
            }
        }
        else
        {
            return Result.Fail(ParserErrors.PublicKeysNotFoundOrInvalid);
        }

        if (publicKey.Id.Length > PrismParameters.MaxIdSize)
        {
            return Result.Fail("KeyId exceeds the maximum allowed size according to the global PrismParameters.");
        }

        if (publicKey.Usage == KeyUsage.UnknownKey)
        {
            return Result.Fail("The UnknownKey is not a valid key usage.");
        }

        return Result.Ok(new PrismPublicKey(
            keyUsage: Enum.Parse<PrismKeyUsage>(publicKey.Usage.ToString()),
            keyId: publicKey.Id,
            keyX: x,
            keyY: y,
            curve: publicKey.EcKeyData is not null ? publicKey.EcKeyData.Curve : publicKey.CompressedEcKeyData!.Curve
        ));
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
            return Result.Fail("Service number exceeds the maximum allowed number of services according to the global PrismParameters.");
        }

        foreach (var service in didData.Services)
        {
            var serviceParseResult = ParseServiceInternal(service, _logger);
            if (serviceParseResult.IsFailed)
            {
                return serviceParseResult.ToResult();
            }

            services.Add(serviceParseResult.Value);
        }

        if (services.Select(p => p.ServiceId).Distinct().Count() != services.Count)
        {
            return Result.Fail("Duplicate service Ids detected. Each key Id must be unique.");
        }

        return services;
    }

    private static Result<PrismService> ParseServiceInternal(Service service, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(service.Type) || !service.Type.Trim().Equals(service.Type) || service.Type.Length > PrismParameters.MaxTypeSize)
        {
            return Result.Fail("Service type is not valid. It must not be empty, must not contain leading or trailing whitespaces and must not exceed the maximum allowed size according to the global PrismParameters.");
        }

        if (!PrismParameters.ExpectedServiceTypes.Contains(service.Type))
        {
            logger.LogWarning($"Service type '{service.Type}' is not in the list of expected service types");
        }

        if (string.IsNullOrWhiteSpace(service.Id) || service.Id.Length > PrismParameters.MaxIdSize)
        {
            return Result.Fail("Service id is not valid. It must not be empty and must not exceed the maximum allowed size according to the global PrismParameters.");
        }

        if (service.ServiceEndpoint.Length > PrismParameters.MaxServiceEndpointSize)
        {
            return Result.Fail("Service endpoint is not valid. It must not exceed the maximum allowed size according to the global PrismParameters.");
        }

        var parsedServiceEndpoint = PrismServiceEndpoints.Parse(service.ServiceEndpoint);
        if (parsedServiceEndpoint.IsFailed)
        {
            return parsedServiceEndpoint.ToResult();
        }

        return Result.Ok(new PrismService(
            serviceId: service.Id,
            type: service.Type,
            prismPrismServiceEndpoints: parsedServiceEndpoint.Value
        ));
    }
}