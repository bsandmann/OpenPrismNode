namespace OpenPrismNode.Sync.Commands.GetPostgresBlockTip;

using Core.DbSyncModels;
using FluentResults;
using MediatR;

public class GetPostgresBlockTipRequest : IRequest<Result<Block>>
{
}