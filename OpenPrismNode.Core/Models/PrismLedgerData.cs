namespace OpenPrismNode.Core.Models;

using System.Text.Json.Serialization;

public sealed class PrismLedgerData
{
    [JsonConstructor]
    public PrismLedgerData(string transactionId, LedgerType ledgerType, LedgerTimestampInfo timestampInfo)
    {
        TransactionId = transactionId;
        TimestampInfo = timestampInfo;
        LedgerType = ledgerType;
    }

    public string TransactionId { get; }
    public LedgerType LedgerType { get; }
    public LedgerTimestampInfo TimestampInfo { get; }
}