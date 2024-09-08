namespace OpenPrismNode.Core.Models.DidDocument;

public static class DidResolutionHeader
{
    public static readonly string DefaultHeader = "*/*";
    public static readonly string ApplicationDidLdJson = "application/did+ld+json";
    public static readonly string ApplicationLdJsonProfile = "application/ld+json;profile=\"https://w3id.org/did-resolution\"";

    public static AcceptedContentType ParseAcceptHeader(string? acceptHeader)
    {
        if (string.IsNullOrEmpty(acceptHeader) ||
            acceptHeader.Contains(DefaultHeader, StringComparison.InvariantCultureIgnoreCase))
        {
            return AcceptedContentType.DidResolutionResult;
        }
        else if (string.IsNullOrEmpty(acceptHeader) ||
                 acceptHeader.Contains(ApplicationDidLdJson, StringComparison.InvariantCultureIgnoreCase))
        {
            return AcceptedContentType.DidDocument;
        }
        else if (acceptHeader.Contains(ApplicationLdJsonProfile, StringComparison.InvariantCultureIgnoreCase))
        {
            return AcceptedContentType.DidResolutionResult;
        }
        else
        {
            return AcceptedContentType.Other;
        }
    }
}