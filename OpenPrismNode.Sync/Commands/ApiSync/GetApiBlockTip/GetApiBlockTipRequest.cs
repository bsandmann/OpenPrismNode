namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

/// <summary>
/// Request to retrieve the latest block (the blockchain tip) from the Blockfrost API.
/// </summary>
public class GetApiBlockTipRequest : IRequest<Result<Block>>
{
    // No parameters needed for this request
}