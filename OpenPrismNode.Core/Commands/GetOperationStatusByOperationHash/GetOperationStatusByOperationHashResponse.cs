namespace OpenPrismNode.Core.Commands.GetOperationStatusByOperationHash;

using OpenPrismNode.Core.Models;

public class GetOperationStatusByOperationHashResponse
{
    /// <summary>
    /// PK - hash of the fully signed operation
    /// </summary>
    public byte[] OperationStatusId { get; init; }

    /// <summary>
    /// Identifier
    /// Hash of the Prism-operation which has the specific status
    /// </summary>
    public byte[] OperationHash { get; init; } 

    /// <summary>
    /// Created
    /// </summary> 
    public DateTime CreatedUtc { get; init; }

    /// <summary>
    /// Last updated
    /// </summary> 
    public DateTime? LastUpdatedUtc { get; init; }

    /// <summary>
    /// Status of the operation
    /// </summary>
    public OperationStatusEnum Status { get; init; }

    /// <summary>
    /// Type of operation which is described by this Status / OperationHash
    /// </summary>
    public OperationTypeEnum OperationType { get; init; } 
    
    /// <summary>
    /// TransactionId for the transaction written on chain
    /// </summary>
    public string TransactionId { get; init; }
}