namespace OpenPrismNode.Core.Commands.GetOperationHashOfCreateDidOperation;

using FluentResults;
using MediatR;

public class GetOperationHashOfCreateDidOperationRequest : IRequest<Result<byte[]>>
{
    public GetOperationHashOfCreateDidOperationRequest(string didIdentifier)
    {
        DidIdentifier = didIdentifier;
    }

    public string DidIdentifier { get; }
}