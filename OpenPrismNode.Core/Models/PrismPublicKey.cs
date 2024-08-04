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
        if (keyX is null || keyY is null)
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

    public static Result<(byte[], byte[])> Decompress(byte[] compressedEcKeyData, string curve)
    {
        if (!curve.Equals(PrismParameters.Secp256k1CurveName, StringComparison.Ordinal))
        {
            return Result.Fail("Only secp256k1 is supported");
        }

        if (!compressedEcKeyData.Length.Equals(33))
        {
            return Result.Fail("Compressed key data must be 33 bytes long");
        }

        var crv = SecNamedCurves.GetByName(curve);
        var point = crv.Curve.DecodePoint(compressedEcKeyData);
        var x = point.XCoord.GetEncoded();
        var y = point.YCoord.GetEncoded();

        if (x.Length.Equals(32) && y.Length.Equals(32))
        {
            return (x, y);
        }
        else
        {
            return Result.Fail("Decompressed key data must be 32 bytes long");
        }
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