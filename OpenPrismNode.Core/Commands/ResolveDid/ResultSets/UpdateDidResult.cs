namespace OpenPrismNode.Core.Commands.ResolveDid.ResultSets;

/// <summary>
/// Result of of Select operation againt the db
/// </summary>
public class UpdateDidResult
{
    /// <summary>
    /// Identifier
    /// Hash of the updateDid operation
    /// </summary>
    public byte[] OperationHash { get; set; } = null!;

    /// <summary>
    /// Identifier
    /// Reference to the connected Did
    /// </summary>
    public byte[] Did { get; set; } = null!;

    /// <summary>
    /// List of all the publicKeys which should be added
    /// </summary>
    public List<PublicKeyResult> PublicKeysToAdd { get; set; } = new List<PublicKeyResult>();

    public List<Tuple<String, int>> PublicKeysToRemove { get; set; } = new List<Tuple<string, int>>();

    public List<ServiceResult> Services { get; set; } = new List<ServiceResult>();
    
    public List<PatchedContextResult> PatchedContextResults { get; set; } = new List<PatchedContextResult>();

    /// <summary>
    /// Blockheight of the block the transaction was embedded in
    /// </summary>
    public long? BlockHeight { get; set; }

    /// <summary>
    /// The sequenceNumber of the transaction inside the block
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The operationsequence inside the transaction
    /// </summary>
    public int OperationSequenceNumber { get; set; }

    /// <summary>
    /// TransactionHash of the Transaction the createDid operation was embedded in
    /// </summary>
    public byte[] TransactionHash { get; set; } = null!;

    /// <summary>
    /// Time when the block was created on the blockchain
    /// </summary>
    public DateTime TimeUtc { get; set; }
    
}