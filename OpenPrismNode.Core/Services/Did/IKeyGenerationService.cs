using OpenPrismNode.Core.Models;

namespace OpenPrismNode.Core.Services.Did;

public interface IKeyGenerationService
{
    List<string> GenerateRandomMnemonic();
    (PrismKeyPair prismKeyPair, string seedHex) GenerateMasterKeyFromMnemonic(List<string> mnemonic);
    PrismKeyPair DeriveKeyFromSeed(string seedHex, int didIndex, PrismKeyUsage keyType, int keyIndex, string keyId, string curve);
}