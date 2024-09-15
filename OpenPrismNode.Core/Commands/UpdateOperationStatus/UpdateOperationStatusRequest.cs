namespace OpenPrismNode.Core.Commands.UpdateOperationStatus;

using FluentResults;
using MediatR;
using Models;

public class UpdateOperationStatusRequest : IRequest<Result>
{
    public UpdateOperationStatusRequest(byte[] operationStatusId, OperationStatusEnum status)
    {
        OperationStatusId = operationStatusId;
        Status = status;
    }

    public OperationStatusEnum Status { get; set; }
    public byte[] OperationStatusId { get; set; }
}