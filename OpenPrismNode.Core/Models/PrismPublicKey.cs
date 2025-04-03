namespace OpenPrismNode.Core.Models;

using System;
using System.Linq;
using Common;
using FluentResults;
using Org.BouncyCastle.Asn1.Sec;

// For PrismKeyUsage
// For PrismParameters

public class PrismPublicKey
{
    public PrismKeyUsage KeyUsage { get; }
    public string KeyId { get; }
    public string Curve { get; } // e.g., "secp256k1", "Ed25519", "X25519"
    public byte[]? X { get; } // For secp256k1 (32 bytes)
    public byte[]? Y { get; } // For secp256k1 (32 bytes)
    public byte[]? RawBytes { get; } // For Ed25519 (32 bytes), X25519 (32 bytes)

    // Constructor for secp256k1
    public PrismPublicKey(PrismKeyUsage keyUsage, string keyId, string curve, byte[] x, byte[] y)
    {
        if (!curve.Equals(PrismParameters.Secp256k1CurveName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"This constructor is for {PrismParameters.Secp256k1CurveName} only. Curve provided: {curve}", nameof(curve));
        if (x == null || x.Length != 32) throw new ArgumentException("X coordinate must be 32 bytes.", nameof(x));
        if (y == null || y.Length != 32) throw new ArgumentException("Y coordinate must be 32 bytes.", nameof(y));

        var hex = PrismEncoding.PublicKeyPairByteArraysToHex(x!, y);

        KeyUsage = keyUsage;
        KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
        Curve = PrismParameters.Secp256k1CurveName; // Normalize
        X = x;
        Y = y;
        Hex = hex;
        LongByteArray = PrismEncoding.HexToByteArray(hex);
        RawBytes = null;
    }

    // Constructor for Ed25519/X25519 (using RawBytes)
    public PrismPublicKey(PrismKeyUsage keyUsage, string keyId, string curve, byte[] rawBytes)
    {
        var lowerCurve = curve;
        if (lowerCurve != PrismParameters.Ed25519CurveName && lowerCurve != PrismParameters.X25519CurveName)
            throw new ArgumentException($"This constructor is for {PrismParameters.Ed25519CurveName} or {PrismParameters.X25519CurveName} only. Curve provided: {curve}", nameof(curve));
        if (rawBytes == null || rawBytes.Length != 32)
            throw new ArgumentException($"Raw public key bytes must be 32 bytes long for {curve}.", nameof(rawBytes));

        KeyUsage = keyUsage;
        KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
        Curve = curve.Equals(PrismParameters.Ed25519CurveName, StringComparison.OrdinalIgnoreCase) ? PrismParameters.Ed25519CurveName : PrismParameters.X25519CurveName; // Normalize
        RawBytes = rawBytes;
        LongByteArray = PrismEncoding.HexToByteArray(PrismEncoding.PublicKeyPairByteArraysToHex(rawBytes, null));
        X = null;
        Y = null;
    }

    public static Result<(byte[], byte[])> Decompress(byte[] compressedEcKeyData, string curve)
    {
        if (curve != "secp256k1")
        {
            Result.Fail("Only secp256k1 is supported");
        }

        try
        {
            var crv = SecNamedCurves.GetByName(curve);
            var point = crv.Curve.DecodePoint(compressedEcKeyData);
            var x = point.XCoord.GetEncoded();
            var y = point.YCoord.GetEncoded();
            return (x, y);
        }
        catch (Exception ex)
        {
            return Result.Fail("Failed to decompress public key: " + ex.Message);
        }
    }

    /// <summary>
    /// Gets the public key bytes in the standard format for the curve.
    /// Secp256k1: 65 bytes uncompressed (0x04 + X + Y)
    /// Ed25519: 32 bytes raw
    /// X25519: 32 bytes raw
    /// </summary>
    public byte[] GetBytes()
    {
        return Curve switch
        {
            PrismParameters.Secp256k1CurveName => new byte[] { 0x04 }.Concat(X!).Concat(Y!).ToArray(),
            PrismParameters.Ed25519CurveName => RawBytes!,
            PrismParameters.X25519CurveName => RawBytes!,
            _ => throw new InvalidOperationException($"Unknown curve type: {Curve}")
        };
    }

    public string KeyYAsHex()
    {
        return PrismEncoding.ByteArrayToHex(this.Y);
    }

    public string LongByteArrayAsHex()
    {
        return PrismEncoding.ByteArrayToHex(this.LongByteArray);
    }

    public string Hex { get; }

    public byte[] LongByteArray { get; }

    public static CompressedECKeyData CompressPublicKey(byte[] x, byte[] y, string curve)
    {
        if (curve != "secp256k1")
        {
            throw new Exception("Only secp256k1 is supported");
        }

        byte[] newArray = new byte[x.Length + 1];
        x.CopyTo(newArray, 1);
        newArray[0] = (byte)(2 + (y[^1] & 1));
        var pk = new CompressedECKeyData()
        {
            Curve = "secp256k1",
            Data = PrismEncoding.ByteArrayToByteString(newArray),
        };
        return pk;
    }
}