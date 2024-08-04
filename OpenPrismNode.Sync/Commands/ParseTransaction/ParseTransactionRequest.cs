namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using FluentResults;
using MediatR;
using Models;

public class ParseTransactionRequest : IRequest<Result<OperationResultWrapper>>
{
    public ParseTransactionRequest(SignedAtalaOperation signedAtalaOperation, int index, ResolveMode? resolveMode = null)
    {
        SignedAtalaOperation = signedAtalaOperation;
        Index = index;
        ResolveMode = resolveMode;
    }

    public SignedAtalaOperation SignedAtalaOperation { get; }

    public int Index { get; set; }

    /// <summary>
    /// Definition of how a Did might be resolved
    /// Can be null, depending on the operation. A Create DID operation does not need to be resolved
    /// </summary>
    public ResolveMode? ResolveMode { get; }
}