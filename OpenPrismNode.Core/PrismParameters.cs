namespace OpenPrismNode.Core;

/// <summary>
/// Versioning and protocol parameters according to <see cref="https://github.com/input-output-hk/prism-did-method-spec/blob/main/w3c-spec/PRISM-method.md#versioning-and-protocol-parameters"/>
/// </summary>
public class PrismParameters
{
    public const int MaxVerifiactionMethodNumber = 50;
    public const int MaxIdSize = 50;
    public const int MaxTypeSize = 100;
    public const int MaxServiceNumber = 50;
    public const int MaxServiceEndpointSize = 300;
    public const string ServiceTypeLinkedDomains = "LinkedDomains";
    public const string ServiceTypeDIDCommMessaging = "DIDCommMessaging";
    public const string ServiceTypeCredentialRegistry = "CredentialRegistry";
    public const string ServiceTypeOID4VCI = "OID4VCI";
    public const string ServiceTypeOID4VP = "OID4VP";
    public static readonly List<string> ExpectedServiceTypes = new List<string> { ServiceTypeLinkedDomains, ServiceTypeDIDCommMessaging, ServiceTypeCredentialRegistry, ServiceTypeOID4VCI, ServiceTypeOID4VP };
    public const string Secp256k1CurveName = "secp256k1";
    public const string Ed25519CurveName = "Ed25519";
    public const string X25519CurveName = "X25519";
    public const string JsonLdDefaultContext = "https://www.w3.org/ns/did/v1";
    public const string JsonLdJsonWebKey2020 = "https://w3id.org/security/suites/jws-2020/v1";
    public const string JsonLdDidCommMessaging = "https://didcomm.org/messaging/contexts/v2";
    public const string JsonLdLinkedDomains = "https://identity.foundation/.well-known/did-configuration/v1";
    public const string SetDidDocument = "setDidDocument";
    public const string AddToDidDocument = "addToDidDocument";
    public const string RemoveFromDidDocument = "removeFromDidDocument";

 
}