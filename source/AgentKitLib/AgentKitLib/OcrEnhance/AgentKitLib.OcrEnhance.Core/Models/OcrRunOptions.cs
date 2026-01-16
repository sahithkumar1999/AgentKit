using System.Text.Json.Serialization;

namespace AgentKitLib.OcrEnhance.Core.Models;

public sealed class OcrRunOptions
{
    [JsonPropertyName("runEnhancement")]
    public bool RunEnhancement { get; set; } = true;

    [JsonPropertyName("includeOriginal")]
    public bool IncludeOriginal { get; set; } = true;

    [JsonPropertyName("saveTxt")]
    public bool SaveTxt { get; set; } = true;

    [JsonPropertyName("saveJson")]
    public bool SaveJson { get; set; } = true;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "eng";
}