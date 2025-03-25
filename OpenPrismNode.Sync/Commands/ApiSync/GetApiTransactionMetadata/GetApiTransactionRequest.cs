namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Request to retrieve transaction metadata from the Blockfrost API using a transaction hash.
/// </summary>
public class GetApiTransactionRequest : IRequest<Result<Transaction?>>
{
    public GetApiTransactionRequest(string txHash, int currentBlockNo, int currentApiBlockTip)
    {
        TxHash = txHash;
        CurrentBlockNo = currentBlockNo;
        CurrentApiBlockTip = currentApiBlockTip;
    }

    /// <summary>
    /// The transaction hash to look up (hex encoded string).
    /// </summary>
    public string TxHash { get; }

    public int CurrentBlockNo { get; }

    public int CurrentApiBlockTip { get; set; }
}