namespace OpenPrismNode.Core.Services.Did;

using Models;

public interface IKeyGenerationService
{
    public List<string> GenerateRandomMnemonic();
    public (PrismKeyPair prismKeyPair, string seedHex) GenerateMasterKeyFromMnemonic(List<string> mnemonic);
    public PrismKeyPair DeriveKeyFromSeed(string seed, int didIndex, PrismKeyUsage keyType, int keyIndex, string keyId);
}