namespace OpenPrismNode.Sync.Commands.GetPostgresTransactionInsideBlock;

using FluentResults;
using MediatR;
using OpenPrismNode.Sync.PostgresModels;

public class GetPostgresTransactionsInsideBlockRequest : IRequest<Result<List<Transaction>>>
{
    public GetPostgresTransactionsInsideBlockRequest(long blockId)
    {
        BlockId = blockId;
    }

    public long BlockId { get; }
}