namespace OpenPrismNode.Sync.Commands.DbSync.GetPostgresBlockTip;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.DbSyncModels;

public class GetPostgresBlockTipRequest : IRequest<Result<Block>>
{
}