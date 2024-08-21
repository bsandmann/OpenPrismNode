namespace OpenPrismNode.Core.Commands.CreateBlock;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Entities;
using OpenPrismNode.Core.Models;

/// <summary>
/// Request
/// </summary>
public class CreateBlockRequest : IRequest<Result<BlockEntity>>
{
    /// <summary>
    /// Request
    /// </summary>
    /// <param name="ledgerType"></param>
    /// <param name="blockHash"></param>
    /// <param name="previousBlockHash"></param>
    /// <param name="blockHeight"></param>
    /// <param name="previousBlockHeight"></param>
    /// <param name="epochNumber"></param>
    /// <param name="timeUtc"></param>
    /// <param name="txCount"></param>
    public CreateBlockRequest(LedgerType ledgerType, Hash blockHash, Hash? previousBlockHash, int blockHeight, int? previousBlockHeight, int epochNumber, DateTime timeUtc, int txCount, bool isFork = false)
    {
        ledger = ledgerType;
        BlockHeight = blockHeight;
        BlockHash = blockHash;
        PreviousBlockHeight = previousBlockHeight;
        PreviousBlockHash = previousBlockHash;
        EpochNumber = epochNumber;
        TimeUtc = timeUtc;
        TxCount = txCount;
        IsFork = isFork;
    }

    /// <summary>
    /// Hash of the block as hex
    /// </summary>
    public Hash BlockHash { get; }

    /// <summary>
    /// Hash of the previous block as hex
    /// </summary>
    public Hash? PreviousBlockHash { get; }
    
    /// <summary>
    /// Ledger (testnet, preprod, mainnet)
    /// </summary>
    public LedgerType ledger { get; }

    /// <summary>
    /// Height of the block (blocknumber)
    /// </summary>
    public int BlockHeight { get; }

    /// <summary>
    /// Height of the previous block (blocknumber)
    /// </summary> 
    public int? PreviousBlockHeight { get; }

    /// <summary>
    /// Epoch
    /// </summary>
    public int EpochNumber { get; }

    /// <summary>
    /// Time when the block was created on the blockchain
    /// </summary>
    public DateTime TimeUtc { get; }

    /// <summary>
    /// Number of transactions in this block
    /// </summary>
    public int TxCount { get; } 
    
    /// <summary>
    /// Flag that this block is a part of a fork
    /// </summary>
    public bool IsFork { get; }

}