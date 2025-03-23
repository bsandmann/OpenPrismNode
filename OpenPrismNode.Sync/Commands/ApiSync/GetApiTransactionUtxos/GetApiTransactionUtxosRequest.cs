namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionUtxos;

using FluentResults;
using MediatR;

/// <summary>
/// Request to retrieve a transaction's UTXOs by its hash from the Blockfrost API.
/// </summary>
public class GetApiTransactionUtxosRequest : IRequest<Result<ApiResponseUtxo>>
{
    /// <summary>
    /// The transaction hash to retrieve UTXOs for.
    /// </summary>
    public string TxHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionUtxosRequest"/> class.
    /// </summary>
    /// <param name="txHash">The transaction hash</param>
    public GetApiTransactionUtxosRequest(string txHash)
    {
        TxHash = txHash;
    }
}