namespace OpenPrismNode.Core.Commands.ResolveDid.Transform;

using Common;
using Models;
using Models.DidDocument;

public static class TransformToDidDocument
{
    public static DidDocument Transform(InternalDidDocument internalDidDocument, LedgerType ledger, bool includeNetworkIdentifier, bool showMasterAndRevocationKeys)
    {
        var networkIdentifier = string.Empty;
        if (includeNetworkIdentifier)
        {
            if (ledger == LedgerType.CardanoMainnet)
            {
                networkIdentifier = "mainnet:";
            }
            else if (ledger == LedgerType.CardanoPreprod)
            {
                networkIdentifier = "preprod:";
            }
        }

        var did = $"did:prism:{networkIdentifier}{internalDidDocument.DidIdentifier}";

        var verificationMethods = new List<VerificationMethod>();
        var publicKeys = internalDidDocument.PublicKeys.Where(p => p.KeyUsage != PrismKeyUsage.MasterKey && p.KeyUsage != PrismKeyUsage.RevocationKey);
        if (showMasterAndRevocationKeys)
        {
            publicKeys = internalDidDocument.PublicKeys.ToList();
        }

        foreach (var prismPublicKey in publicKeys)
        {
            verificationMethods.Add(new VerificationMethod()
            {
                Id = $"{did}#{prismPublicKey.KeyId}",
                Type = "JsonWebKey2020",
                Controller = did,
                PublicKeyJwk = new PublicKeyJwk()
                {
                    Curve = prismPublicKey.Curve,
                    KeyType = GetKeyType(prismPublicKey.Curve),
                    X = PrismEncoding.ByteArrayToBase64(prismPublicKey.KeyX),
                    Y = prismPublicKey.KeyY is not null ? PrismEncoding.ByteArrayToBase64(prismPublicKey.KeyY) : null
                }
            });
        }

        var authentication = new List<string>();
        foreach (var prismPublicKey in internalDidDocument.PublicKeys.Where(p => p.KeyUsage == PrismKeyUsage.AuthenticationKey))
        {
            authentication.Add($"{did}#{prismPublicKey.KeyId}");
        }

        var assertionMethods = new List<string>();
        foreach (var prismPublicKey in internalDidDocument.PublicKeys.Where(p => p.KeyUsage == PrismKeyUsage.IssuingKey))
        {
            assertionMethods.Add($"{did}#{prismPublicKey.KeyId}");
        }

        var keyAgreement = new List<string>();
        foreach (var prismPublicKey in internalDidDocument.PublicKeys.Where(p => p.KeyUsage == PrismKeyUsage.KeyAgreementKey))
        {
            keyAgreement.Add($"{did}#{prismPublicKey.KeyId}");
        }

        var services = new List<DidDocumentService>();
        foreach (var prismService in internalDidDocument.PrismServices)
        {
            services.Add(GetServiceEndpoint(prismService, did));
        }

        var didDocument = new DidDocument
        {
            Context = internalDidDocument.Contexts,
            Id = did,
            VerificationMethod = verificationMethods.Any() ? verificationMethods : null,
            Authentication = authentication.Any() ? authentication : null,
            AssertionMethod = assertionMethods.Any() ? assertionMethods : null,
            KeyAgreement = keyAgreement.Any() ? keyAgreement : null,
            Service = internalDidDocument.PrismServices.Any() ? services : null
        };

        return didDocument;
    }

    private static string GetKeyType(string curve)
    {
        return curve switch
        {
            "secp256k1" => "EC",
            "Ed25519" => "OKP",
            "X25519" => "OKP",
        };
    }

    private static DidDocumentService GetServiceEndpoint(PrismService ps, string did)
    {
        var service = new DidDocumentService();
        service.Id = $"{did}#{ps.ServiceId}";
        if (ps.ServiceEndpoints.Uri is not null)
        {
            service.ServiceEndpointString = ps.ServiceEndpoints.Uri.AbsoluteUri;
        }
        else if (ps.ServiceEndpoints.ListOfUris is not null)
        {
            service.ServiceEndpointStringList = ps.ServiceEndpoints.ListOfUris.Select(u => u.AbsoluteUri).ToList();
        }
        else if (ps.ServiceEndpoints.Json is not null)
        {
            service.ServiceEndpointObject = ps.ServiceEndpoints.Json;
        }

        return service;
    }
}