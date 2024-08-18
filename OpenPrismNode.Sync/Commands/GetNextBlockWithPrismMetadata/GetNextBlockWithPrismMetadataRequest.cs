using FluentResults;
using MediatR;

public class GetNextBlockWithPrismMetadataRequest : IRequest<Result<GetNextBlockWithPrismMetadataResponse>>
{
    public GetNextBlockWithPrismMetadataRequest(int startBlockHeight, int metadataKey, int maxBlockHeight)
    {
        StartBlockHeight = startBlockHeight;
        MetadataKey = metadataKey;
        MaxBlockHeight = maxBlockHeight;
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
}