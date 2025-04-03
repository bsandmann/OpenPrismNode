namespace OpenPrismNode.Core.Commands.ResolveDid.Transform;

using System.Text.Json;
using Common;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Models;
using Models.DidDocument;
using OpenPrismNodeService;

public class TransformToPrismGrpcResponse
{
    public static GetDidDocumentResponse Transform(InternalDidDocument internalDidDocument)
    {
        var didData = TransformToDidData(internalDidDocument.DidIdentifier, internalDidDocument.PublicKeys, internalDidDocument.PrismServices, internalDidDocument.Contexts);
        var response = new GetDidDocumentResponse
        {
            Document = new DIDData()
            {
                Id = didData.Id,
                PublicKeys = { didData.PublicKeys },
                Services = { didData.Services },
                Context = { didData.Context }
            },
            // the versionId contians the hex-encoded operationHash of the last operation effecting the DID
            LastUpdateOperation = PrismEncoding.HexToByteString(internalDidDocument.VersionId),
            LastSyncedBlockTimestamp = Timestamp.FromDateTime(internalDidDocument.Updated ?? internalDidDocument.Created),
        };

        return response;
    }

    private static DIDData TransformToDidData(string didIdentifier, List<PrismPublicKey> publicKeys, List<PrismService> services, List<string> contexts)
    {
        var didData = new DIDData();
        didData.Id = didIdentifier;
        // Add public keys
        foreach (var prismPublicKey in publicKeys)
        {
            var publicKey = new PublicKey
            {
                Id = prismPublicKey.KeyId,
                Usage = (KeyUsage)prismPublicKey.KeyUsage
            };

            if (prismPublicKey.Curve == PrismParameters.Secp256k1CurveName)
            {
                if (prismPublicKey.Y != null && prismPublicKey.X.Length == 32)
                {
                    // Uncompressed key
                    publicKey.EcKeyData = new ECKeyData()
                    {
                        Curve = prismPublicKey.Curve,
                        X = ByteString.CopyFrom(prismPublicKey.X),
                        Y = ByteString.CopyFrom(prismPublicKey.Y)
                    };
                }
                else
                {
                    // Compressed key
                    publicKey.CompressedEcKeyData = new CompressedECKeyData()
                    {
                        Curve = prismPublicKey.Curve,
                        Data = ByteString.CopyFrom(prismPublicKey.X)
                    };
                }
            }
            else if (prismPublicKey.Curve == PrismParameters.Ed25519CurveName ||
                     prismPublicKey.Curve == PrismParameters.X25519CurveName)
            {
                publicKey.EcKeyData = new ECKeyData
                {
                    Curve = prismPublicKey.Curve,
                    X = ByteString.CopyFrom(prismPublicKey.X ?? prismPublicKey.RawBytes)
                    // Y is not used for these curves
                };
            }
            else
            {
                throw new Exception($"Unsupported curve: {prismPublicKey.Curve}");
            }

            didData.PublicKeys.Add(publicKey);
        }


        // Add services
        foreach (var prismService in services)
        {
            var service = new Service
            {
                Id = prismService.ServiceId,
                Type = prismService.Type,
                ServiceEndpoint = SerializeServiceEndpoints(prismService.ServiceEndpoints)
            };

            didData.Services.Add(service);
        }

        foreach (var context in contexts)
        {
            if (!context.Equals(PrismParameters.JsonLdDefaultContext) &&
                !context.Equals(PrismParameters.JsonLdJsonWebKey2020) &&
                !context.Equals(PrismParameters.JsonLdDidCommMessaging) &&
                !context.Equals(PrismParameters.JsonLdLinkedDomains))
            {
                didData.Context.Add(context);
            }
        }

        return didData;
    }

    private static string SerializeServiceEndpoints(ServiceEndpoints serviceEndpoints)
    {
        if (serviceEndpoints.Uri != null)
        {
            // Single URI
            return serviceEndpoints.Uri.AbsoluteUri;
        }
        else if (serviceEndpoints.ListOfUris != null)
        {
            // List of URIs
            var uriStrings = serviceEndpoints.ListOfUris.Select(uri => uri.AbsoluteUri).ToList();
            return JsonSerializer.Serialize(uriStrings);
        }
        else if (serviceEndpoints.Json != null)
        {
            // JSON object
            return JsonSerializer.Serialize(serviceEndpoints.Json);
        }
        else
        {
            throw new Exception("Invalid ServiceEndpoints: No data to serialize.");
        }
    }
}