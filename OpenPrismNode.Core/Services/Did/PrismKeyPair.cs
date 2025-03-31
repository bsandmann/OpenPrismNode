namespace OpenPrismNode.Core.Services.Did;

using System.Text.Json.Serialization;
using OpenPrismNode.Core.Models;
using PrismPublicKey = Models.PrismPublicKey;

public sealed class PrismKeyPair
{
    [JsonConstructor]
    public PrismKeyPair(PrismKeyUsage keyUsage, PrismPrivateKey privateKey, Models.PrismPublicKey publicKey)
    {
        PrivateKey = privateKey;
        PublicKey = publicKey;
        KeyUsage = keyUsage;
    }

    public PrismKeyUsage KeyUsage { get; }

    public PrismPrivateKey PrivateKey { get; }

    public PrismPublicKey PublicKey { get; }
}