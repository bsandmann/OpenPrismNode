﻿namespace OpenPrismNode.Core.Commands.ResolveDid;

using FluentResults;
using MediatR;
using Models;
using ResolveDidResponse = ResolveDid.ResolveDidResponse;

public class ResolveDidRequest : IRequest<Result<ResolveDid.ResolveDidResponse>>
{
    /// <summary>
    /// Constructor for the Resolve-Requset to resolve a DID
    /// </summary>
    /// <param name="did"></param>
    /// <param name="blockHeight">Setting this value to null, means to consider all available information up to the current point int time</param>
    /// <param name="blockSequence">Setting this value to null, means to consider all available information up to the current point int time</param>
    /// <param name="operationSequence">Setting this value to null, means to consider all available information up to the current point int time</param>
    /// <exception cref="ArgumentException"></exception>
    public ResolveDidRequest(string didIdentifier, long? blockHeight = null, int? blockSequence = null, int? operationSequence = null)
    {
        DidIdentifier = didIdentifier;
        BlockHeight = blockHeight;
        BlockSequence = blockSequence;
        OperationSequence = operationSequence;

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
}