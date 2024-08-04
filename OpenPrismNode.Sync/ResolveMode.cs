namespace OpenPrismNode.Sync;

public class ResolveMode
{
    // public ResolveMode()
    // {
    //     if (resolveMode == ParserResolveMode.NoResolveNoSignatureVerification)
    //     {
    //         // ParserResolveMode = ParserResolveMode.NoResolveNoSignatureVerification;
    //         BlockHeight = null;
    //         BlockSequence = null;
    //         OperationSequence = null;
    //     }
    //     else
    //     {
    //         throw new ArgumentException("When resolving a Did, the blockHeight, blockSequence and operationSequence must be provided");
    //     }
    // }

    /// <summary>
    /// What information should be considered, when resolving a did?
    /// </summary>
    /// <param name="resolveMode"></param>
    /// <param name="blockHeight">When null: consider all inforation available in the db</param>
    /// <param name="blockSequence">When null: consider all inforation available in the db</param>
    /// <param name="operationSequence">When null: consider all inforation available in the db</param>
    /// <exception cref="AggregateException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public ResolveMode(long? blockHeight, int? blockSequence, int? operationSequence)
    {
        // if (resolveMode == ParserResolveMode.ResolveAgainstDatabaseAndVerifySignature)
        // {
        BlockHeight = blockHeight;
        BlockSequence = blockSequence;
        OperationSequence = operationSequence;
        // ParserResolveMode = ParserResolveMode.ResolveAgainstDatabaseAndVerifySignature;

        if (blockHeight is null && (blockSequence is not null || operationSequence is not null))
        {
            throw new ArgumentException("If providing blockSequence or operationSequnce, the blockHeigt has also be provided");
        }
        else if (blockSequence is null && operationSequence is not null)
        {
            throw new ArgumentException("If providing operationSequnce, the blockHeigt and  blockSequence has also be provided");
        }
        // }
        // else
        // {
        //     throw new AggregateException("When not resolving a did, don't provide values for blochheight, blockSequence and operationSequence");
        // }
    }


    // public ParserResolveMode ParserResolveMode { get; }

    /// <summary>
    /// The last blockHeight to include into the Resolving-Process
    /// A block contains multiple transactions
    /// </summary>
    public long? BlockHeight { get; }

    /// <summary>
    /// Include every transaction inside a block NOT INCLUDING this number (Every Transaction has a block-sequence number)
    /// The Transaction with the specified BlockSequence will not be included
    /// </summary>
    public int? BlockSequence { get; }

    /// <summary>
    /// Inside a transaction there can be multiple Operations. Include every Operation BELOW this number
    /// The Operation with the specified OperationsSequence will note be included
    /// </summary>
    public int? OperationSequence { get; }
}