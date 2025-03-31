namespace OpenPrismNode.Core.Services.Did;

using System;
using System.Linq;
using System.Security.Cryptography;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models.Keys;

public class Slip0010DerivationServiceCardanoSharp : ISlip0010DerivationService
{
    // Implement the new interface method returning only private key
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
        const int SeedLength = 64;
        const int KeyLength = 32;
        const int ExtendedKeyLength = 64;

        if (seedBytes == null || seedBytes.Length != SeedLength)
        {
            throw new ArgumentException($"Seed must be {SeedLength} bytes long.", nameof(seedBytes));
        }
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("m/"))
        {
            throw new ArgumentException("Invalid derivation path.", nameof(path));
        }

        try
        {
            byte[] masterScalarBytes = seedBytes[..KeyLength];
            byte[] masterChainCodeBytes = seedBytes[KeyLength..];

            byte[] scalarHash;
            using (var sha512 = SHA512.Create())
            {
                scalarHash = sha512.ComputeHash(masterScalarBytes);
            }
            byte[] masterNonceBytes = scalarHash[KeyLength..];
            byte[] extendedMasterKeyBytes = masterScalarBytes.Concat(masterNonceBytes).ToArray();

            if (extendedMasterKeyBytes.Length != ExtendedKeyLength)
            {
                 throw new InvalidOperationException("Internal error: Constructed extended master key is not 64 bytes.");
            }

            var masterNodePrivateKey = new PrivateKey(extendedMasterKeyBytes, masterChainCodeBytes);
            PrivateKey derivedNodePrivateKey = masterNodePrivateKey.Derive(path);

            // Extract ONLY the final 32-byte private key scalar
            byte[] finalPrivateKeyBytes = derivedNodePrivateKey.Key[..KeyLength];

            if (finalPrivateKeyBytes.Length != KeyLength)
            {
                 throw new InvalidOperationException($"Derived private key length is incorrect: {finalPrivateKeyBytes.Length}");
            }

            return finalPrivateKeyBytes; // Return only the private key
        }
        catch (ArgumentException ex)
        {
             throw new ArgumentException($"Failed to derive key for path '{path}'. Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during SLIP-0010 derivation for path {path}: {ex}");
            throw new InvalidOperationException($"An unexpected error occurred during key derivation for path '{path}'.", ex);
        }
    }
}