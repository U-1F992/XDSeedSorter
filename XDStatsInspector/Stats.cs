using System.Text.Json.Serialization;

public class Stats
{
#pragma warning disable CS8618
    [JsonPropertyName("hp")]
    public int HP { get; set; }
    [JsonPropertyName("attack")]
    public int Attack { get; set; }
    [JsonPropertyName("defense")]
    public int Defense { get; set; }
    [JsonPropertyName("speed")]
    public int Speed { get; set; }
    [JsonPropertyName("spAtk")]
    public int SpAtk { get; set; }
    [JsonPropertyName("spDef")]
    public int SpDef { get; set; }
#pragma warning restore CS8618
}
