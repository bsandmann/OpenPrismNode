namespace OpenPrismNode.Sync.Commands.DecodeTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode;

public class DecodeTransactionRequest : IRequest<Result<List<SignedAtalaOperation>>>
{
    public DecodeTransactionRequest(string json)
    {
        Json = json;
    }

    public string Json { get; }
}