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
    public static readonly List<string> ExpectedServiceTypes = new List<string> { "LinkedDomains", "DIDCommMessaging", "CredentialRegistry", "OID4VCI", "OID4VP" };
    public const string Secp256k1CurveName = "secp256k1";
    public const string Ed25519CurveName = "edd25519";
    public const string X25519CurveName = "x25519";
}