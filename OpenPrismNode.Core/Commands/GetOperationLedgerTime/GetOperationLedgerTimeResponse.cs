namespace OpenPrismNode.Core.Commands.GetOperationLedgerTime;

public class GetOperationLedgerTimeResponse
{
    public int LedgerTimeBlockHeight { get; init; }
    public int LedgerTimeBlockSequence { get; init; }
    public int LedgerTimeOperationSequence { get; init; }
}