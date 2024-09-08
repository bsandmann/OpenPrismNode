namespace OpenPrismNode.Core.Models.DidDocument;

using System.Text.Json.Serialization;

/// <summary>
/// Metadata about the DID-Document itself, creation time, updated, deactiviated, etc.
/// See Core-spec: https://www.w3.org/TR/did-core/#did-document-metadata
/// And also Spec-registry: https://www.w3.org/TR/did-spec-registries/#did-document-metadata
/// </summary>
public record DidDocumentMetadata
{
    /// <summary>
    /// This property is identical to the EquivalentId property except it is associated with a single value rather than a set, and the DID is defined to be the canonical ID for the DID subject within the scope of the containing DID document.
    /// In the Cardano-case it is the shortform of the did
    /// </summary>
    [JsonPropertyName("canonicalId")]
    public string CanonicalId { get; init; } = string.Empty;

    /// <summary>
    /// The timestamp of the creation operation. It should be a string formatted as an XML Datetime normalized to UTC 00:00:00 and without sub-second decimal precision.
    /// The is the timestamp of the first time the did was created, not the timestop of the DidDocument itself!
    /// In the Cardano-case it is the timestamp of the Cardano block that contained the Create-Operations
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    /// <summary>
    /// The timestamp of the last Update operation for the document version which was resolved. It follows the same formatting rules as the Created property.
    /// If a DID has never been updated, this property is to be omitted.
    /// In the Cardano-case it is the timestamp of the last valid block related to this did
    /// </summary>
    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }

    /// <summary>
    /// This property may indicate the version of the last update operation for the document version which was resolved. The value of the property must be an ASCII string.
    /// In the Cardano-case hash of the of AtalaOperation contained in the latest valid SignedAtalaOperation
    /// </summary>
    [JsonPropertyName("versionId")]
    public string VersionId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether a DID has been deactivated. If a DID is deactivated, the value must be true. If a DID is not deactivated, this property is optional.
    /// </summary>
    [JsonPropertyName("deactivated")]
    public bool? Deactivated { get; set; }

    /// <summary>
    /// This property may contain the timestamp of the next Update operation if the resolved document version is not the latest version of the document.
    /// </summary>
    [JsonPropertyName("nextUpdate")]
    public DateTime? NextUpdate { get; init; }

    /// <summary>
    /// Optional property specific to Cardano which describes the postion of the Crdano transaction in the block (block sequence number)
    /// </summary>
    [JsonPropertyName("cardanoTransactionPosition")]
    public int CardanoTransactionPosition { get; init; }

    /// <summary>
    /// Optional property specific to Cardano which describies the position of the SignedAtalaOperation in an AtalaBLock
    /// </summary>
    [JsonPropertyName("operationPosition")]
    public int OperationPosition { get; init; }

    /// <summary>
    /// Optional property specific to Cardano which describes the transactionId of the Cardano transaction that created the DidDocument
    /// </summary>
    [JsonPropertyName("originTxId")]
    public string OriginTxId { get; init; } = string.Empty;

    /// <summary>
    /// Optional property specific to Cardano which describes the transactionId of the Cardano transaction that updated the DidDocument last
    /// </summary>
    [JsonPropertyName("updateTxId")]
    public string? UpdateTxId { get; init; }

    /// <summary>
    /// Optional property specific to Cardano which describes the transactionId of the Cardano transaction that deactivated the DidDocument
    /// </summary>
    [JsonPropertyName("deactivateTxId")]
    public string? DeactivateTxId { get; init; }
}