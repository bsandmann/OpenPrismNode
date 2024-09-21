namespace OpenPrismNode.Core.Commands.GetOperationStatus;

using Entities;
using FluentResults;
using MediatR;

public class GetOperationStatusRequest : IRequest<Result<OperationStatusEntity>>
{
    public GetOperationStatusRequest(byte[] operationStatusEntityId)
    {
        OperationStatusId = operationStatusEntityId;
    }
    public byte[] OperationStatusId { get; set; } 
}