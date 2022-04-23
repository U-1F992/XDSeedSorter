using System.Text.Json.Serialization;

/// <summary>
/// config.json
/// </summary>
public class Config
{
#pragma warning disable CS8618
    [JsonPropertyName("portName")]
    public string PortName { get; set; }
    [JsonPropertyName("captureIndex")]
    public int CaptureIndex { get; set; }
    [JsonPropertyName("token")]
    public string Token { get; set; }
    [JsonPropertyName("tsv")]
    public UInt32 Tsv { get; set; }
    [JsonPropertyName("targets")]
    public UInt32[] Targets { get; set; }
    [JsonPropertyName("waitTime")]
    public Dictionary<string, TimeSpan> WaitTime { get; set; }
    [JsonPropertyName("advancesPerSecond")]
    public double AdvancesPerSecond { get; set; }
    [JsonPropertyName("allowLoad")]
    public bool AllowLoad { get; set; }
    [JsonPropertyName("advancesByLoading")]
    public int AdvancesByLoading { get; set; }
    [JsonPropertyName("advancesByOpeningItems")]
    public int AdvancesByOpeningItems { get; set; }
#pragma warning restore CS8618
}
/// <summary>
/// <see cref="Config.WaitTime"/> のKey一覧
/// </summary>
public static class WaitTime
{
    public static readonly string Maximum = "maximum";
    public static readonly string Left = "left";
}
