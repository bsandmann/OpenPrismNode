using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class GetNextBlockWithPrismMetadataRequest : IRequest<Result<GetNextBlockWithPrismMetadataResponse>>
{
    public GetNextBlockWithPrismMetadataRequest(int startBlockHeight, int metadataKey, int maxBlockHeight, LedgerType ledger)
    {
        StartBlockHeight = startBlockHeight;
        MetadataKey = metadataKey;
        MaxBlockHeight = maxBlockHeight;
        Ledger = ledger;
    }

    /// <summary>
    /// Block height to start searching from
    /// </summary>
    public int StartBlockHeight { get; }
    
    /// <summary>
    /// The key of the metadata to search for (21325 for PRISM)
    /// </summary>
    public int MetadataKey { get; }
    
    /// <summary>
    /// The maximum block height to search up to
    /// </summary>
    public int MaxBlockHeight { get; }
   
    /// <summary>
    /// Network 
    /// </summary>
    public LedgerType Ledger { get; }
    
}