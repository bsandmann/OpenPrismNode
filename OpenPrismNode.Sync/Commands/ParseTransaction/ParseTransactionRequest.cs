namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using FluentResults;
using MediatR;
using Models;

public class ParseTransactionRequest : IRequest<Result<OperationResultWrapper>>
{
    public ParseTransactionRequest(SignedAtalaOperation signedAtalaOperation, int index, ResolveMode resolveMode)
    {
        SignedAtalaOperation = signedAtalaOperation;
        Index = index;
        ResolveMode = resolveMode;
    }

    public SignedAtalaOperation SignedAtalaOperation { get; }

    public int Index { get; set; }

    /// <summary>
    /// Definition of how a Did might be resolved
    /// </summary>
    public ResolveMode ResolveMode { get; }
}