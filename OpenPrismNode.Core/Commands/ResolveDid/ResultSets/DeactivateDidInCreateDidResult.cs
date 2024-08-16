namespace OpenPrismNode.Core.Commands.ResolveDid.ResultSets;

public class DeactivateDidInCreateDidResult
{
    /// <summary>
    /// Identifier
    /// Hash of the deactivateDid operation
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
}