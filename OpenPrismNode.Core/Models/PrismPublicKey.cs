namespace OpenPrismNode.Core.Models;

using System.Text.Json.Serialization;
using Common;
using FluentResults;
using Org.BouncyCastle.Asn1.Sec;

public sealed class PrismPublicKey
{
    [JsonConstructor]
    public PrismPublicKey(PrismKeyUsage keyUsage, string keyId, string curve, byte[]? keyX, byte[]? keyY)
    {
        if ((curve == PrismParameters.Secp256k1CurveName && (keyX is null || keyY is null)) ||
            (curve == PrismParameters.Ed25519CurveName || curve == PrismParameters.X25519CurveName) && (keyX is null || keyY is not null))
        {
            throw new ArgumentException("Either the compressed or the compressed EcKeyData has to be provided");
        }

        var hex = PrismEncoding.PublicKeyPairByteArraysToHex(keyX!, keyY);
        
        KeyUsage = keyUsage;
        KeyId = keyId;
        KeyX = keyX!;
        KeyY = keyY;
        Curve = curve;
        Hex = hex;
        LongByteArray = PrismEncoding.HexToByteArray(hex);
    }

    public static Result<(byte[], byte[]?)> Decompress(byte[] keyData, string curve)
    {
        if (curve.Equals(PrismParameters.Secp256k1CurveName, StringComparison.Ordinal))
        {
            if (keyData.Length != 33)
            {
                return Result.Fail("Compressed secp256k1 key data must be 33 bytes long");
            }

            var crv = SecNamedCurves.GetByName(curve);
            var point = crv.Curve.DecodePoint(keyData);
            var x = point.XCoord.GetEncoded();
            var y = point.YCoord.GetEncoded();

            if (x.Length == 32 && y.Length == 32)
            {
                return Result.Ok<(byte[],byte[]?)>((x, y));
            }
            else
            {
                return Result.Fail("Decompressed secp256k1 key data must be 32 bytes for each coordinate");
            }
        }
        else if (curve.Equals(PrismParameters.Ed25519CurveName, StringComparison.Ordinal) || curve.Equals(PrismParameters.X25519CurveName, StringComparison.Ordinal))
        {
            if (keyData.Length != 32)
            {
                return Result.Fail("ED25519/X25519 key data must be 32 bytes long");
            }
        
            // For ED25519, we return the key as-is for the x-coordinate, and null for the y-coordinate
            return Result.Ok<(byte[],byte[]?)>((keyData, null));
        }
        else
        {
            return Result.Fail($"Unsupported curve: {curve}");
        }
    }

    public string KeyId { get; }
    public PrismKeyUsage KeyUsage { get; }

    public byte[] KeyX { get; }

    /// <summary>
    /// Is null for ED25519 and X25519.
    /// Not null for SECP256K1.
    /// </summary>
    public byte[]? KeyY { get; }

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