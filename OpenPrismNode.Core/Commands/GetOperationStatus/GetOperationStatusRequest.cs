namespace OpenPrismNode.Core.Commands.GetOperationStatus;

using Entities;
using FluentResults;
using MediatR;

public class GetOperationStatusRequest : IRequest<Result<OperationStatusEntity>>
{
    public GetOperationStatusRequest(byte[] operationStatusId)
    {
        OperationStatusId = operationStatusId;
    }
    public byte[] OperationStatusId { get; set; } 
}