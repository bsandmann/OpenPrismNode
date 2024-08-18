using FluentResults;
using MediatR;

public class GetNextBlockWithPrismMetadataResponse
{
    public int? BlockHeight { get; set; }
    public int? EpochNumber { get; set; }
}