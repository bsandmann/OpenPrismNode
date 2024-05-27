namespace OpenPrismNode.Sync.Commands.GetPostgresBlockTip;

using FluentResults;
using MediatR;
using PostgresModels;

public class GetPostgresBlockTipRequest : IRequest<Result<Block>>
{
}