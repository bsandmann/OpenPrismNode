namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiTransactionMetadata;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Request to retrieve transaction metadata from the Blockfrost API using a transaction hash.
/// </summary>
public class GetApiTransactionMetadataRequest : IRequest<Result<Transaction>>
{
    /// <summary>
    /// The transaction hash to look up (hex encoded string).
    /// </summary>
    public string TxHash { get; set; } = string.Empty;
}