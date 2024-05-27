namespace OpenPrismNode.Sync.Commands.GetPostgresTransactionInsideBlock;

using Dapper;
using FluentResults;
using MediatR;
using PostgresModels;
using Services;

public class GetPostgresTransactionsInsideBlockHandler : IRequestHandler<GetPostgresTransactionsInsideBlockRequest, Result<List<Transaction>>>
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public GetPostgresTransactionsInsideBlockHandler(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<Result<List<Transaction>>> Handle(GetPostgresTransactionsInsideBlockRequest request, CancellationToken cancellationToken)
    {
        await using (var connection = _connectionFactory.CreateConnection())
        {
            string commandText = $"SELECT t.* FROM tx t join tx_metadata m on t.id = m.tx_id where t.block_id = {request.BlockId} and m.key = 21325";
            var transactions = await connection.QueryAsync<Transaction>(commandText);
            //TODO verifiy ordering
            return transactions.OrderBy(p => p.block_index).ToList();
        }
    }
}