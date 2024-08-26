namespace OpenPrismNode.Core.Commands.ResolveDid.ResultSets;

/// <summary>
/// Result of of Select operation againt the db
/// </summary>
public class CreateDidResult
{
    /// <summary>
    /// Identifier
    /// Hash of the createDid operation and also the did itself
    /// </summary>
    public byte[] OperationHash { get; set; } = null!;
    
    /// <summary>
    /// The operationSequence inside the transaction
    /// </summary>
    public int OperationSequenceNumber { get; set; }
    
    /// <summary>
    /// TransactionHash of the Transaction the createDid operation was embedded in
    /// </summary>
    public byte[] TransactionHash { get; set; } = null!;
    
    /// <summary>
    /// Blockheight of the block the transaction was embedded in
    /// </summary>
    public int? BlockHeight { get; set; }
    
    /// <summary>
    /// The sequenceNumber of the transaction inside the block
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Time when the block was created on the blockchain
    /// </summary>
    public DateTime TimeUtc { get; set; }

    /// <summary>
    /// List of all the publicKeys which where created with the did
    /// </summary>
    public List<PublicKeyResult> PublicKeys { get; set; } = new List<PublicKeyResult>();
    
    /// <summary>
    /// List of all the services which where created with the did
    /// </summary>
    public List<ServiceResult> Services { get; set; } = new List<ServiceResult>();
    
    /// <summary>
    /// Optional List of additional Contexts
    /// </summary>
    public List<string>? PatchedContexts { get; set; }
    
    public DeactivateDidInCreateDidResult? DeactivateDid { get; set; }
}