namespace OpenPrismNode.Core.Models;

using System.Text.Json.Serialization;

public sealed class LedgerTimestampInfo
{
    [JsonConstructor]
    public LedgerTimestampInfo(uint blockSequenceNumber, uint operationSequenceNumber, DateTime blockTimestamp)
    {
        BlockSequenceNumber = blockSequenceNumber;
        OperationSequenceNumber = operationSequenceNumber;
        BlockTimestamp = blockTimestamp;
    }

    public static IEqualityComparer<LedgerTimestampInfo> LedgerTimestampInfoComparer { get; } = new LedgerTimestampInfoEqualityComparer();
    
    /// <summary>
    /// The transaction index inside the underlying block.
    /// </summary>
    public uint BlockSequenceNumber { get;  }
    
    /// <summary>
    /// The operation index inside the AtalaBlock. 
    /// </summary>
    public uint OperationSequenceNumber { get;  }
    
    /// <summary>
    /// The timestamp provided from the underlying blockchain.
    /// </summary>
    public DateTime BlockTimestamp { get;  }

    private sealed class LedgerTimestampInfoEqualityComparer : IEqualityComparer<LedgerTimestampInfo>
    {
        public bool Equals(LedgerTimestampInfo? x, LedgerTimestampInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.BlockSequenceNumber == y.BlockSequenceNumber && x.OperationSequenceNumber == y.OperationSequenceNumber && x.BlockTimestamp.Equals(y.BlockTimestamp);
        }

        public int GetHashCode(LedgerTimestampInfo obj)
        {
            return HashCode.Combine(obj.BlockSequenceNumber, obj.OperationSequenceNumber, obj.BlockTimestamp);
        }
    }
}