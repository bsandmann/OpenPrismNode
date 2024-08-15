namespace OpenPrismNode.Core.Commands.CreateTransaction;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class CreateTransactionRequest : IRequest<Result>
{
    public CreateTransactionRequest(Hash transactionHash, Hash blockHash, int blockHeight, int transactionFee, int transactionSize, int transactionIndex, OperationResultWrapper parsingResult, List<UtxoWrapper> utxos)
    {
        TransactionHash = transactionHash;
        BlockHash = blockHash;
        BlockHeight = blockHeight;
        TransactionFees = transactionFee;
        TransactionSize = transactionSize;
        TransactionIndex = transactionIndex;
        ParsingResult = parsingResult;
        Utxos = utxos;
    }


    public Hash TransactionHash { get; }

    public Hash BlockHash { get; }
    public int BlockHeight { get; }
    public int TransactionSize { get; }
    public int TransactionFees { get; }
    public int TransactionIndex { get; }

    /// <summary>
    /// Result of the successful parsing operation
    /// </summary>
    public OperationResultWrapper ParsingResult { get; }

    /// <summary>
    /// List of incoming and outgoing Utxos with value and wallet addresses
    /// </summary>
    public List<UtxoWrapper> Utxos { get; }
}