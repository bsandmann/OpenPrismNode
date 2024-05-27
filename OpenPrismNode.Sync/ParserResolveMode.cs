namespace OpenPrismNode.Sync;

public enum ParserResolveMode
{
    /// <summary>
    /// The default. Parsing e.g. an UpdateDiD Operation or a IssueCredential-Operation requires that the signature
    /// of that operation has to be checked. This can only be done by resolving the DID first to get to that
    /// signature. This again requires access to a database
    /// </summary>
    ResolveAgainstDatabaseAndVerifySignature,
    
    /// <summary>
    /// Skip the resolving and thus skip checking the signatures of a parsed transaction
    /// Only for testting purposes!
    /// </summary>
    NoResolveNoSignatureVerication,
}