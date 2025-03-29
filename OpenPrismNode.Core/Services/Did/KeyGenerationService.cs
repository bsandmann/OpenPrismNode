namespace OpenPrismNode.Core.Services.Did;

using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;

public sealed class KeyGenerationService : IKeyGenerationService
{
    private readonly IHdKeyService _hdKeyService;
    private readonly IEcService? _ecService;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hdKeyService"></param>
    /// <param name="ecService">The ecService is only need to verify that Keys are correct. It can be omitted if required</param>
    public KeyGenerationService(IHdKeyService hdKeyService, IEcService? ecService = null)
    {
        _hdKeyService = hdKeyService;
        _ecService = ecService;
    }

    public List<string> GenerateRandomMnemonic()
    {
        return _hdKeyService.GenerateRandomMnemonic();
    }

    public (PrismKeyPair prismKeyPair, string seedHex) GenerateMasterKeyFromMnemonic(List<string> mnemonic)
    {
        var passphrase = "passphrase";
        var seedHex = _hdKeyService.DeriveSeed(mnemonic, passphrase);

        var masterKey = DeriveKeyFromSeed(seedHex, 0, PrismKeyUsage.MasterKey, 0, "master0");

        return (new PrismKeyPair(
            keyUsage: PrismKeyUsage.MasterKey,
            privateKey: masterKey.PrivateKey,
            publicKey: masterKey.PublicKey
        ), seedHex);
    }

    public PrismKeyPair DeriveKeyFromSeed(string seed, int didIndex, PrismKeyUsage keyType, int keyIndex, string keyId)
    {
        var keyTypeIndex = keyType switch
        {
            PrismKeyUsage.MasterKey => 0,
            PrismKeyUsage.IssuingKey => 1,
            PrismKeyUsage.KeyAgreementKey => 2,
            PrismKeyUsage.AuthenticationKey => 3,
            PrismKeyUsage.RevocationKey => 4,
            PrismKeyUsage.CapabilityInvocationKey => 5,
            PrismKeyUsage.CapabilityDelegationKey => 6,
            _ => throw new NotSupportedException()
        };
        var path = $"m/{didIndex}'/{keyTypeIndex}'/{keyIndex}'";

        (byte[] privateKey, byte[] publicKey) = _hdKeyService.DeriveChildKeys(seed, path);
        var publicKeyDeconstructed = PrismEncoding.HexToPublicKeyPairByteArrays(PrismEncoding.ByteArrayToHex(publicKey));
        if (_ecService is not null)
        {
            var check = _ecService.CheckKeys(privateKey, publicKey);
            if (!check)
            {
                throw new Exception("KeyGeneration unsuccessful");
            }
        }

        return new PrismKeyPair(
            keyUsage: keyType,
            privateKey: new PrismPrivateKey(privateKey),
            publicKey: new Did.PrismPublicKey(keyType, keyId,"secp256k1", publicKeyDeconstructed.Value.x, publicKeyDeconstructed.Value.y)
        );
    }
}