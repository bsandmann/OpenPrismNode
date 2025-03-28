namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiNextBlockWithPrismMetadata;

using DbSync.GetNextBlockWithPrismMetadata;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class GetApiNextBlockWithPrismMetadataRequest : IRequest<Result<GetNextBlockWithPrismMetadataResponse>>
{
    public GetApiNextBlockWithPrismMetadataRequest(int startBlockHeight, int metadataKey, int maxBlockHeight, LedgerType ledger, int currentApiBlockTip)
    {
        StartBlockHeight = startBlockHeight;
        MetadataKey = metadataKey;
        MaxBlockHeight = maxBlockHeight;
        Ledger = ledger;
        CurrentApiBlockTip = currentApiBlockTip;
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

    /// <summary>
    /// Tip of the chain according to Blockfrost
    /// </summary>
    public int CurrentApiBlockTip { get; }

}