namespace OpenPrismNode.Core.Commands.UpdateOperationStatus;

using FluentResults;
using MediatR;
using Models;

public class UpdateOperationStatusRequest : IRequest<Result>
{
    public UpdateOperationStatusRequest(int operationStatusEntityId, OperationStatusEnum status)
    {
        OperationStatusEntityId = operationStatusEntityId;
        Status = status;
    }

    public OperationStatusEnum Status { get; }
    public int OperationStatusEntityId { get; }
}