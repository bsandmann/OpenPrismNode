namespace OpenPrismNode.Core.Commands.CreateOperationsStatus;

using FluentResults;
using MediatR;
using Models;

public class CreateOperationStatusRequest : IRequest<Result<byte[]>>
{
    public CreateOperationStatusRequest(byte[] operationStatusId, byte[] operationHash, OperationStatusEnum status, OperationTypeEnum operationType)
    {
        OperationStatusId = operationStatusId;
        OperationHash = operationHash;
        Status = status;
        OperationType = operationType;
    }
    
    /// <summary>
    /// Key of the operation status
    /// Hash of the fully signed operation
    /// </summary>
    public byte[] OperationStatusId { get; set; } = null!;
    
    /// <summary>
    /// Identifier
    /// Hash of the Prism-operation which has the specific status
    /// Different from OperationStatusId
    /// </summary>
    public byte[] OperationHash { get; set; } = null!;
    
    /// <summary>
    /// Status of the operation
    /// </summary>
    public OperationStatusEnum Status { get; set; }
    
    /// <summary>
    /// Reference to type of operation
    /// </summary>
    public OperationTypeEnum OperationType { get; set; } 
}