namespace OpenPrismNode.Core.Models.DidDocument;

/// <summary>
/// Metadata from the resolution process itself
/// https://w3c.github.io/did-core/#did-resolution-metadata
/// https://w3c-ccg.github.io/did-resolution/#output-resolutionmetadata
/// </summary>
public record DidResolutionMetadata
{
    /// <summary>
    /// Required property of the contentType e.g. "application/did+ld+json"
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// If the resolution failed, this property must be set 
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Addtional error information, which is not part of the spec, but may be present if the ResolutionError is set
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolvers
    /// </summary>
    public DateTime? Retrieved { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolvers
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolvers
    /// </summary>
    public string? DriverUrl { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolvers
    /// </summary>
    public long? Duration { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolvers
    /// </summary>
    public IDictionary<string, object>? Did { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolversm
    /// </summary>
    public string? ConvertedFrom { get; init; }

    /// <summary>
    /// Common property of universal resolvers. Not a standard, but used by many resolversm
    /// </summary>
    public string? ConvertedTo { get; init; }
}