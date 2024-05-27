namespace OpenPrismNode.Sync.Commands.EncodeTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode;

public class EncodeTransactionRequest : IRequest<Result<string>>
{
    public EncodeTransactionRequest(List<SignedAtalaOperation> signedAtalaOperations)
    {
        SignedAtalaOperations = signedAtalaOperations;
    }

    public List<SignedAtalaOperation> SignedAtalaOperations { get; }
}