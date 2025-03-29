namespace OpenPrismNode.Core.Crypto;

public interface IHdKeyService
{
    public List<string> GenerateRandomMnemonic();

    public string DeriveSeed(List<string> mnemonic, string passphrase);

    public (byte[] privateKey, byte[] publicKey) DeriveChildKeys(string seedHex, string path);

}