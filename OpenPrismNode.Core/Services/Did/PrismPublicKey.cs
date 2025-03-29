namespace OpenPrismNode.Core.Services.Did;

using System.Text.Json.Serialization;
using EnsureThat;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Models;
using Org.BouncyCastle.Asn1.Sec;

public sealed class PrismPublicKey
{
    [JsonConstructor]
    public PrismPublicKey(PrismKeyUsage keyUsage, string keyId, string curve, byte[]? keyX, byte[]? keyY)
    {
        if (keyX is not null && keyY is not null)
        {
            Ensure.That(keyX.Length).Is(32);
            Ensure.That(keyY.Length).Is(32);
        }
        else
        {
            throw new ArgumentException("Either the compressed or the compressed EcKeyData has to be provided");
        }

        KeyUsage = keyUsage;
        KeyId = keyId;
        KeyX = keyX;
        KeyY = keyY;
        Curve = curve;
        Hex = PrismEncoding.PublicKeyPairByteArraysToHex(KeyX, KeyY);
        LongByteArray = PrismEncoding.HexToByteArray(PrismEncoding.PublicKeyPairByteArraysToHex(KeyX, KeyY));
    }

    public string KeyId { get; }
    public PrismKeyUsage KeyUsage { get; }

    public byte[] KeyX { get; }

    public byte[] KeyY { get; }

    public string Hex { get; }

    public byte[] LongByteArray { get; }

    public string Curve { get; }

    public string KeyXAsHex()
    {
        return PrismEncoding.ByteArrayToHex(this.KeyX);
    }

    public string KeyYAsHex()
    {
        return PrismEncoding.ByteArrayToHex(this.KeyY);
    }

    public string LongByteArrayAsHex()
    {
        return PrismEncoding.ByteArrayToHex(this.LongByteArray);
    }
}