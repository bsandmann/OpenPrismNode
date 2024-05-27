namespace OpenPrismNode.Core.Models;

public sealed record DidDocument
{
    public DidDocument(string didIdentifierIdentifier, List<PrismPublicKey> publicKeys, List<PrismService> prismServices, List<string> contexts)
    {
        DidIdentifier = didIdentifierIdentifier;
        PublicKeys = publicKeys;
        PrismServices = prismServices;
        Contexts = contexts;
    }

    public string DidIdentifier { get; }
    public List<PrismPublicKey> PublicKeys { get; }
    public List<PrismService> PrismServices { get; }
    public List<string> Contexts { get; }
}