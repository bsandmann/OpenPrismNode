namespace OpenPrismNode.Core.Commands.ResolveDid;

using FluentResults;
using MediatR;
using Models;

public class ResolveDidRequest : IRequest<Result<ResolveDid.ResolveDidResponse>>
{
    /// <summary>
    /// Constructor for the Resolve-Requset to resolve a DID
    /// </summary>
    /// <param name="ledger"></param>
    /// <param name="didIdentifier"></param>
    /// <param name="blockHeight">Setting this value to null, means to consider all available information up to the current point int time. Providing a value insted resolves up to that point in time, allowing for the resolving past events correctly.</param>
    /// <param name="blockSequence">Setting this value to null, means to consider all available information up to the current point int time. Providing a value insted resolves up to that point in time, allowing for the resolving past events correctly. </param>
    /// <param name="operationSequence">Setting this value to null, means to consider all available information up to the current point int time. Providing a value insted resolves up to that point in time, allowing for the resolving past events correctly.</param>
    /// <exception cref="ArgumentException"></exception>
    public ResolveDidRequest(LedgerType ledger, string didIdentifier, long? blockHeight = null, int? blockSequence = null, int? operationSequence = null)
    {
        DidIdentifier = didIdentifier;
        BlockHeight = blockHeight;
        BlockSequence = blockSequence;
        OperationSequence = operationSequence;
        Ledger = ledger;

        if (blockHeight is null && (blockSequence is not null || operationSequence is not null))
        {
            throw new ArgumentException("If providing blockSequence or operationSequnce, the blockHeigt has also be provided");
        }
        else if (blockSequence is null && operationSequence is not null)
        {
            throw new ArgumentException("If providing operationSequnce, the blockHeigt and  blockSequence has also be provided");
        }
    }

    public string DidIdentifier { get; }

    /// <summary>
    /// The last blockHeigt TO INLCUDE into the Resolving-Process
    /// A block contains multiple transactions
    /// </summary>
    public long? BlockHeight { get; }

    /// <summary>
    /// Inlcude every transaction inside a block NOT INCLUDING this number (Every Transaction has a block-sequence number)
    /// The Transaction with the specified BlockSequence will not be included
    /// </summary>
    public int? BlockSequence { get; }

    /// <summary>
    /// Inside a transaction there can be multiple Operations. Include every Operation BELOW this number
    /// The Operation with the specifiged OperationsSeuqnde will note be included
    /// </summary>
    public int? OperationSequence { get; }
    
    public LedgerType Ledger { get; }
}