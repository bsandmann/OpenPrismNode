using System.Text.Json.Serialization;

public class NetworkInformationResponse
{
    [JsonPropertyName("sync_progress")]
    public SyncProgress SyncProgress { get; set; }

    [JsonPropertyName("node_tip")]
    public NodeTip NodeTip { get; set; }

    [JsonPropertyName("network_tip")]
    public NetworkTip NetworkTip { get; set; }

    [JsonPropertyName("next_epoch")]
    public NextEpoch NextEpoch { get; set; }

    [JsonPropertyName("node_era")]
    public string NodeEra { get; set; }

    [JsonPropertyName("network_info")]
    public NetworkInfo NetworkInfo { get; set; }

    [JsonPropertyName("wallet_mode")]
    public string WalletMode { get; set; }
}