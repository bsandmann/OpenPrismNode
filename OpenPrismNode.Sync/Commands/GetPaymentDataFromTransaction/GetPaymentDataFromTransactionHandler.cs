using Dapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenPrismNode.Core.Common;

namespace OpenPrismNode.Sync.Commands.GetPaymentDataFromTransaction;

using Core.DbSyncModels;
using Core.Models;
using Utxo = Core.DbSyncModels.Utxo;

public class GetPaymentDataFromTransactionHandler : IRequestHandler<GetPaymentDataFromTransactionRequest, Result<Payment>>
{
    private readonly string _connectionString;

    public GetPaymentDataFromTransactionHandler(IOptions<AppSettings> appSetting)
    {
        _connectionString = appSetting.Value.PrismNetwork.DbSyncPostgresConnectionString;
    }

    public async Task<Result<Payment>> Handle(GetPaymentDataFromTransactionRequest request, CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var payment = new Payment
        {
            Outgoing = await GetUtxos(connection, request.TxId, isOutgoing: true, cancellationToken),
            Incoming = await GetUtxos(connection, request.TxId, isOutgoing: false, cancellationToken)
        };

        return Result.Ok(payment);
    }

    private async Task<List<Utxo>> GetUtxos(NpgsqlConnection connection, long txId, bool isOutgoing, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT t.index, t.value, t.stake_address_id, t.address, s.view as stake_address
            FROM tx_out t
            LEFT JOIN stake_address s ON t.stake_address_id = s.id
            WHERE t.{0} = @TxId";

        string formattedSql = string.Format(sql, isOutgoing ? "tx_id" : "consumed_by_tx_id");

        var transactions = await connection.QueryAsync<TransactionOutExtended>(formattedSql, new { TxId = txId });

        return transactions.Select(t => new Utxo
        {
            Index = t.index,
            Value = t.value,
            WalletAddress = new WalletAddress
            {
                StakeAddressString = t.stake_address,
                WalletAddressString = t.address
            }
        }).ToList();
    }

    private class TransactionOutExtended : TransactionOut
    {
        public string? stake_address { get; set; }
    }
}