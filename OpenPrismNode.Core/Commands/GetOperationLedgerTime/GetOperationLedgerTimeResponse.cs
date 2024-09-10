namespace OpenPrismNode.Core.Commands.GetOperationLedgerTime;

public class GetOperationLedgerTimeResponse
{
    public int LedgerTimeBlockHeight { get; set; }
    public int LedgerTimeBlockSequence { get; set; }
    public int LedgerTimeOperationSequence { get; set; }
}