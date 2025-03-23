namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionByHash;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels; // Or wherever your domain Transaction model might reside

/// <summary>
/// Request to retrieve a transaction by its hash from the Blockfrost API.
/// </summary>
public class GetApiTransactionByHashRequest : IRequest<Result<Transaction>>
{
    /// <summary>
    /// The transaction hash to retrieve.
    /// </summary>
    public string TxHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiTransactionByHashRequest"/> class.
    /// </summary>
    /// <param name="txHash">The transaction hash</param>
    public GetApiTransactionByHashRequest(string txHash)
    {
        TxHash = txHash;
    }
}