namespace OpenPrismNode.Sync.Commands.DbSync.GetPaymentDataFromTransaction;

using Dapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Core.Models;
using Utxo = Core.DbSyncModels.Utxo;

/// <summary>
/// Retrieves payment data (UTXOs) associated with a specific transaction from the Cardano DB Sync PostgreSQL database.
/// This handler is used to obtain information about transaction inputs and outputs for wallet operations.
/// 
/// Note: This handler directly creates a NpgsqlConnection instead of using INpgsqlConnectionFactory.
/// </summary>
public class GetPaymentDataFromTransactionHandler : IRequestHandler<GetPaymentDataFromTransactionRequest, Result<Payment>>
{
    private readonly string _connectionString;

    public GetPaymentDataFromTransactionHandler(IOptions<AppSettings> appSetting)
    {
        _connectionString = appSetting.Value.PrismLedger.DbSyncPostgresConnectionString;
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

    private async Task<List<Utxo>> GetUtxos(NpgsqlConnection connection, int txId, bool isOutgoing, CancellationToken cancellationToken)
    {
        // SQL Query: Retrieves transaction outputs (UTXOs) related to a transaction
        // - Selects output data including index, value, stake address info, and address
        // - Joins with stake_address table to get the human-readable stake address
        // - Filters by either tx_id (for outgoing UTXOs created by this tx) or 
        //   consumed_by_tx_id (for incoming UTXOs consumed by this tx)
        // - {0} is replaced based on whether we're looking for outgoing or incoming UTXOs
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