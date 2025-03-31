namespace OpenPrismNode.Core.Crypto;

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Services.Did; // For path parsing

public class Slip0010DerivationServiceCustom : ISlip0010DerivationService
{
    private const int KeyLength = 32;
    private const int ChainCodeLength = 32;
    private const int SeedLength = KeyLength + ChainCodeLength; // 64
    private const uint HardenedOffset = 0x80000000;

    public byte[] DerivePrivateKey(string seedHex, string path)
    {
        try
        {
            byte[] seedBytes = Convert.FromHexString(seedHex);
            return DerivePrivateKey(seedBytes, path);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid seed hex string.", nameof(seedHex), ex);
        }
    }

    public byte[] DerivePrivateKey(byte[] seedBytes, string path)
    {
        if (seedBytes == null || seedBytes.Length != SeedLength)
        {
            throw new ArgumentException($"Seed must be {SeedLength} bytes long.", nameof(seedBytes));
        }
        if (string.IsNullOrWhiteSpace(path) || !path.ToLowerInvariant().StartsWith("m"))
        {
            throw new ArgumentException("Invalid derivation path. Must start with 'm'.", nameof(path));
        }

        // Initialize with master key and chain code from the seed
        byte[] currentPrivateKey = seedBytes[..KeyLength];
        byte[] currentChainCode = seedBytes[KeyLength..];

        // Parse path segments (skip 'm')
        string[] segments = path.Split('/');
        if (segments.Length < 2 || segments[0].ToLowerInvariant() != "m")
        {
             throw new ArgumentException("Invalid path format. Expected 'm/...'");
        }

        for (int i = 1; i < segments.Length; i++) // Start from first index after 'm'
        {
            var match = Regex.Match(segments[i], @"^(\d+)(')?$");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid path segment: {segments[i]}", nameof(path));
            }

            if (!uint.TryParse(match.Groups[1].Value, out uint index))
            {
                 throw new ArgumentException($"Invalid index value: {match.Groups[1].Value}", nameof(path));
            }

            bool isHardened = match.Groups[2].Success; // Check if ' is present

            if (!isHardened)
            {
                // SLIP-0010 Ed25519 only supports hardened derivation
                throw new ArgumentException($"Non-hardened derivation is not supported for Ed25519 (SLIP-0010). Segment: {segments[i]}", nameof(path));
            }

            if (index >= HardenedOffset) // Already includes offset? Should not happen with regex.
            {
                 throw new ArgumentException($"Invalid index value (too large): {index}", nameof(path));
            }

            uint hardenedIndex = index | HardenedOffset;

            // Perform hardened derivation step
            (currentPrivateKey, currentChainCode) = DeriveHardenedChild(currentPrivateKey, currentChainCode, hardenedIndex);
        }

        return currentPrivateKey; // Return the final derived private key seed (k)
    }

    private (byte[] ChildKey, byte[] ChildChainCode) DeriveHardenedChild(byte[] parentKey, byte[] parentChainCode, uint hardenedIndex)
    {
        // Data for HMAC: 0x00 || parentKey (k_p) || hardenedIndex (4 bytes, big-endian)
        byte[] data = new byte[1 + KeyLength + 4];
        data[0] = 0x00; // Prefix for hardened derivation
        Buffer.BlockCopy(parentKey, 0, data, 1, KeyLength);

        // Convert index to big-endian bytes
        byte[] indexBytes = BitConverter.GetBytes(hardenedIndex);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(indexBytes);
        }
        Buffer.BlockCopy(indexBytes, 0, data, 1 + KeyLength, 4);

        // Calculate HMAC-SHA512
        byte[] hmacResult;
        using (var hmac = new HMACSHA512(parentChainCode))
        {
            hmacResult = hmac.ComputeHash(data);
        }

        // Split HMAC result
        byte[] childKey = hmacResult[..KeyLength];       // I_L = Child Key Seed (k_c)
        byte[] childChainCode = hmacResult[KeyLength..]; // I_R = Child Chain Code (c_c)

        // Note: SLIP-0010 doesn't involve adding parent key to I_L for Ed25519 hardened keys,
        // unlike BIP32-secp256k1. I_L directly becomes the child key seed.

        return (childKey, childChainCode);
    }
}