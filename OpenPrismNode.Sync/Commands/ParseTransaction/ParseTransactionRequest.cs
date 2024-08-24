namespace OpenPrismNode.Sync.Commands.ParseTransaction;

using Core.Models;
using FluentResults;
using MediatR;
using Models;

public class ParseTransactionRequest : IRequest<Result<OperationResultWrapper>>
{
    public ParseTransactionRequest(SignedAtalaOperation signedAtalaOperation, LedgerType ledger, int index, ResolveMode? resolveMode = null)
    {
        SignedAtalaOperation = signedAtalaOperation;
        Index = index;
        ResolveMode = resolveMode;
        Ledger = ledger;
    }

    public SignedAtalaOperation SignedAtalaOperation { get; }

    public int Index { get; set; }

    /// <summary>
    /// Definition of how a Did might be resolved
    /// Can be null, depending on the operation. A Create DID operation does not need to be resolved
    /// </summary>
    public ResolveMode? ResolveMode { get; }
    
    public LedgerType Ledger { get; }
}