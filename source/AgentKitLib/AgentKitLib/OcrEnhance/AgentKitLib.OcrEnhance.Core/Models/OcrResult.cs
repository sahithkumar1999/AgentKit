using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgentKitLib.OcrEnhance.Core.Models
{
    public sealed class OcrResult
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("meanConfidence")]
        public float? MeanConfidence { get; set; }

        [JsonPropertyName("words")]
        public List<OcrWord> Words { get; set; } = [];

        [JsonPropertyName("engine")]
        public string Engine { get; set; } = "";
    }

    public sealed class OcrWord
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("confidence")]
        public float? Confidence { get; set; }

        // Bounding box in pixels
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("w")]
        public int W { get; set; }

        [JsonPropertyName("h")]
        public int H { get; set; }
    }

    public sealed class OcrEngineOptions
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "eng";
    }
}
