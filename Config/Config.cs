using Sunameri;
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
    [JsonPropertyName("sequences")]
    public Dictionary<string, Operation[]> Sequences { get; set; }
#pragma warning restore CS8618
}
/// <summary>
/// <see cref="Config.Sequences"/> のKey一覧
/// </summary>
public static class Sequences
{
    public static readonly string Reset = "reset";
    public static readonly string MoveQuickBattle = "moveQuickBattle";
    public static readonly string LoadParties = "loadParties";
    public static readonly string DiscardParties = "discardParties";
    public static readonly string EntryToBattle = "entryToBattle";
    public static readonly string ExitBattle = "exitBattle";
    public static readonly string MoveMenu = "moveMenu";
    public static readonly string MoveOptions = "moveOptions";
    public static readonly string EnableVibration = "enableVibration";
    public static readonly string DisableVibration = "disableVibration";
    public static readonly string MoveContinue = "moveContinue";
    public static readonly string Load = "load";
    public static readonly string MoveSave = "moveSave";
    public static readonly string Save = "save";
    public static readonly string MoveItems = "moveItems";
    public static readonly string OpenCloseItems = "openCloseItems";
    public static readonly string WatchSteps = "watchSteps";
    public static readonly string Finalize = "finalize";
}
/// <summary>
/// <see cref="Config.WaitTime"/> のKey一覧
/// </summary>
public static class WaitTime
{
    public static readonly string Maximum = "maximum";
    public static readonly string Left = "left";
}
