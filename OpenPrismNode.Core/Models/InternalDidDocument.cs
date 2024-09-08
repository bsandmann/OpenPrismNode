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
    public InternalDidDocument(string didIdentifierIdentifier, List<PrismPublicKey> publicKeys, List<PrismService> prismServices, List<string> contexts, DateTime created, string versionId, int cardanoTransactionPosition, int operationPosition, string originTxId, DateTime? updated = null,
        string? updateTxId = null, string? deactivateTxId = null)
    {
        DidIdentifier = didIdentifierIdentifier;
        PublicKeys = publicKeys;
        PrismServices = prismServices;
        Contexts = contexts;
        Created = created;
        Updated = updated;
        VersionId = versionId;
        CardanoTransactionPosition = cardanoTransactionPosition;
        OperationPosition = operationPosition;
        OriginTxId = originTxId;
        UpdateTxId = updateTxId;
        DeactivateTxId = deactivateTxId;
    }

    public string DidIdentifier { get; }
    public List<PrismPublicKey> PublicKeys { get; }
    public List<PrismService> PrismServices { get; }
    public List<string> Contexts { get; set; }

    /// <summary>
    /// The timestamp of the Cardano block that contained the Create-Operations
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// The timestamp of the last valid block related to this did
    /// </summary>
    public DateTime? Updated { get; set; }

    /// <summary>
    /// In the Cardano-case hash of the of AtalaOperation contained in the latest valid SignedAtalaOperation
    /// </summary>
    public string VersionId { get; set; }

    /// <summary>
    /// Deactivated DID
    /// </summary>
    public bool Deactivated { get; set; }

    /// <summary>
    /// Optional property which describes the postion of the Cardano transaction in the block (block sequence number)
    /// </summary>
    public int CardanoTransactionPosition { get; set; }

    /// <summary>
    /// Optional property which describies the position of the SignedAtalaOperation in an AtalaBLock
    /// </summary>
    public int OperationPosition { get; set; }

    /// <summary>
    /// The transactionId of the Cardano transaction that created the DidDocument 
    /// </summary>
    public string OriginTxId { get; init; }

    /// <summary>
    /// Optional: The transactionId of the Cardano transaction that updated the DidDocument last
    /// </summary>
    public string? UpdateTxId { get; set; }

    /// <summary>
    /// Optional: The transactionId of the Cardano transaction that deactivated the DidDocument
    /// </summary>
    public string? DeactivateTxId { get; set; }
}