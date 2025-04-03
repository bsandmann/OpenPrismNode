namespace OpenPrismNode.Core.Commands.GetOperationStatusByOperationHash;

using FluentResults;
using MediatR;

public class GetOperationStatusByOperationHashRequest : IRequest<Result<GetOperationStatusByOperationHashResponse>>
{
    public GetOperationStatusByOperationHashRequest(byte[] operationHash)
    {
        OperationHash = operationHash;
    }
    public byte[] OperationHash { get; }
}