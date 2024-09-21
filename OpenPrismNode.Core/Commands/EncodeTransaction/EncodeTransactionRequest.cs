namespace OpenPrismNode.Core.Commands.EncodeTransaction;

using FluentResults;
using Grpc.Models;
using MediatR;
using OpenPrismNode;

public class EncodeTransactionRequest : IRequest<Result<TransactionModel>>
{
    public EncodeTransactionRequest(List<SignedAtalaOperation> signedAtalaOperations)
    {
        SignedAtalaOperations = signedAtalaOperations;
    }

    public List<SignedAtalaOperation> SignedAtalaOperations { get; }
}