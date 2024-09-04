namespace OpenPrismNode.Core.Models;

public sealed record InternalDidDocument
{
    /// <summary>
    /// Simplified representation of a DID Document focus on PRISM related information
    /// </summary>
    /// <param name="didIdentifierIdentifier"></param>
    /// <param name="publicKeys"></param>
    /// <param name="prismServices"></param>
    /// <param name="contexts"></param>
    public InternalDidDocument(string didIdentifierIdentifier, List<PrismPublicKey> publicKeys, List<PrismService> prismServices, List<string> contexts)
    {
        DidIdentifier = didIdentifierIdentifier;
        PublicKeys = publicKeys;
        PrismServices = prismServices;
        Contexts = contexts;
    }

    public string DidIdentifier { get; }
    public List<PrismPublicKey> PublicKeys { get; }
    public List<PrismService> PrismServices { get; }
    public List<string> Contexts { get; set; }
}