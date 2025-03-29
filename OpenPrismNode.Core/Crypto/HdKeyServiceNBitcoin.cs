namespace OpenPrismNode.Core.Crypto;

using global::NBitcoin;

public sealed class HdKeyServiceNBitcoin : IHdKeyService
{
    public List<string> GenerateRandomMnemonic()
    {
        Mnemonic mnemo = new Mnemonic(Wordlist.English, WordCount.TwentyFour);
        return mnemo.Words.ToList();
    }

    public string DeriveSeed(List<string> mnemonic, string passphrase)
    {
        var concat = string.Join(' ', mnemonic);
        var mnemo = new Mnemonic(concat, Wordlist.English);
        var seedHex = ByteArrayToHex(mnemo.DeriveSeed(passphrase));
        return seedHex;
    }

    public (byte[] privateKey, byte[] publicKey) DeriveChildKeys(string seedHex, string path)
    {
        ExtKey parent = new ExtKey(seedHex: seedHex);
        ExtKey child = parent.Derive(new KeyPath(path));
        var privateKey = child.PrivateKey.ToBytes();
        var publicKey = child.GetPublicKey().Decompress().ToBytes();
        return (privateKey, publicKey);
    }

    public string ByteArrayToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}