using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenPrismNode.Core.Models.CardanoWallet;

public class TransactionDetailsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("amount")]
    public Amount Amount { get; set; }

    [JsonPropertyName("fee")]
    public Amount Fee { get; set; }

    [JsonPropertyName("deposit_taken")]
    public Amount DepositTaken { get; set; }

    [JsonPropertyName("deposit_returned")]
    public Amount DepositReturned { get; set; }

    [JsonPropertyName("inserted_at")]
    public TimeInfo InsertedAt { get; set; }

    [JsonPropertyName("expires_at")]
    public ExpiresAt ExpiresAt { get; set; }

    [JsonPropertyName("pending_since")]
    public TimeInfo PendingSince { get; set; }

    [JsonPropertyName("depth")]
    public Depth Depth { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; }

    [JsonPropertyName("inputs")]
    public List<TransactionInputOutput> Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public List<TransactionInputOutput> Outputs { get; set; }

    [JsonPropertyName("collateral")]
    public List<TransactionInputOutput> Collateral { get; set; }

    [JsonPropertyName("collateral_outputs")]
    public List<TransactionInputOutput> CollateralOutputs { get; set; }

    [JsonPropertyName("withdrawals")]
    public List<Withdrawal> Withdrawals { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement Metadata { get; set; }

    [JsonPropertyName("script_validity")]
    public string ScriptValidity { get; set; }

    [JsonPropertyName("certificates")]
    public List<object> Certificates { get; set; }

    [JsonPropertyName("mint")]
    public object Mint { get; set; }

    [JsonPropertyName("burn")]
    public object Burn { get; set; }

    [JsonPropertyName("validity_interval")]
    public ValidityInterval ValidityInterval { get; set; }

    [JsonPropertyName("script_integrity")]
    public List<string> ScriptIntegrity { get; set; }

    [JsonPropertyName("extra_signatures")]
    public List<string> ExtraSignatures { get; set; }
}