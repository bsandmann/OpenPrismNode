namespace OpenPrismNode.Core.Commands.GetOperationStatus;

using Models;

public class GetOperationStatusResponse
{
    /// <summary>
    /// PK - hash of the fully signed operation
    /// </summary>
    public byte[] OperationStatusId { get; set; }

    /// <summary>
    /// Identifier
    /// Hash of the Prism-operation which has the specific status
    /// </summary>
    public byte[] OperationHash { get; set; } = null!;

    /// <summary>
    /// Created
    /// </summary> 
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Last updated
    /// </summary> 
    public DateTime? LastUpdatedUtc { get; set; }

    /// <summary>
    /// Status of the operation
    /// </summary>
    public OperationStatusEnum Status { get; set; }

    /// <summary>
    /// Type of operation which is described by this Status / OperationHash
    /// </summary>
    public OperationTypeEnum OperationType { get; set; } 
    
    /// <summary>
    /// TransactionId for the transaction written on chain
    /// </summary>
    public string TransactionId { get; set; }
}