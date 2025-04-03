namespace OpenPrismNode.Core.Services.Did;

public class PrismDidTemplate
{
    public string Method { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string LongFormDocument { get; private set; } = string.Empty;

    public List<string> Mnemonic { get; set; } = new List<string>();
    public string SeedAsHex { get; set; } = string.Empty;
    public PrismKeyPair? MasterKeyPair { get; set; } = null;

    public Dictionary<string, PrismKeyPair> KeyPairs { get; } = new Dictionary<string, PrismKeyPair>();

}