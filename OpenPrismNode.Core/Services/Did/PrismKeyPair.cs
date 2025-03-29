namespace OpenPrismNode.Core.Services.Did;

using System.Text.Json.Serialization;
using OpenPrismNode.Core.Models;
using PrismPublicKey = Did.PrismPublicKey;

public sealed class PrismKeyPair
{
    [JsonConstructor]
    public PrismKeyPair(PrismKeyUsage keyUsage, PrismPrivateKey privateKey, Did.PrismPublicKey publicKey)
    {
        PrivateKey = privateKey;
        PublicKey = publicKey;
        KeyUsage = keyUsage;
    }

    public PrismKeyUsage KeyUsage { get; }

    public PrismPrivateKey PrivateKey { get; }

    public Did.PrismPublicKey PublicKey { get; }
}