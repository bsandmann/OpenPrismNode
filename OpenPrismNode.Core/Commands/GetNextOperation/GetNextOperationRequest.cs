namespace OpenPrismNode.Core.Commands.GetNextOperation;

using FluentResults;
using MediatR;
using Models;

public class GetNextOperationRequest : IRequest<Result<DateTime?>>
{
    public GetNextOperationRequest(byte[] currentOperationHash, LedgerType ledger)
    {
        CurrentOperationHash = currentOperationHash;
        Ledger = ledger;
    }
    
    public byte[] CurrentOperationHash { get;  }
    public LedgerType Ledger { get; }
}