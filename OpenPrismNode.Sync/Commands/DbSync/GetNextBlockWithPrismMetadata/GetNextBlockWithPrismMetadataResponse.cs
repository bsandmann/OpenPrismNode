namespace OpenPrismNode.Sync.Commands.DbSync.GetNextBlockWithPrismMetadata;

public class GetNextBlockWithPrismMetadataResponse
{
    public int? BlockHeight { get; init; }
    public int? EpochNumber { get; init; }
}