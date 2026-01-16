using System.Text.Json.Serialization;

namespace AgentKitLib.OcrEnhance.Core.Models;

/// <summary>
/// Represents the normalized output of an OCR operation.
/// </summary>
/// <remarks>
/// <para>
/// This model is designed to be OCR-engine-agnostic so that different backends (e.g., Tesseract or a cloud OCR
/// service) can emit results in a consistent shape.
/// </para>
/// <para>
/// At minimum, implementations should populate <see cref="Text"/>. Other fields (confidence, word boxes) may be
/// omitted when not supported by the OCR backend.
/// </para>
/// </remarks>
public sealed class OcrResult
{
    /// <summary>
    /// Gets or sets the full recognized text for the image.
    /// </summary>
    /// <remarks>
    /// The text is typically returned in reading order, but ordering is implementation-specific and
    /// depends on the OCR engine.
    /// </remarks>
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets the mean confidence score across the OCR result, when provided by the OCR backend.
    /// </summary>
    /// <remarks>
    /// Confidence scoring is backend-specific. Some engines return a value in the range 0..1, others 0..100.
    /// This library treats it as an opaque value and does not enforce a specific scale.
    /// </remarks>
    [JsonPropertyName("meanConfidence")]
    public float? MeanConfidence { get; set; }

    /// <summary>
    /// Gets or sets word-level OCR results, including text and bounding boxes (when available).
    /// </summary>
    /// <remarks>
    /// Word segmentation and bounding boxes are not guaranteed to be available for all OCR engines.
    /// When not supported, this collection should be empty.
    /// </remarks>
    [JsonPropertyName("words")]
    public List<OcrWord> Words { get; set; } = [];

    /// <summary>
    /// Gets or sets the identifier for the OCR engine that produced this result (e.g., <c>tesseract</c>).
    /// </summary>
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "";
}

/// <summary>
/// Represents a single word (token) recognized by OCR.
/// </summary>
/// <remarks>
/// Bounding box coordinates are expressed in pixels relative to the processed image that was OCR’d.
/// </remarks>
public sealed class OcrWord
{
    /// <summary>
    /// Gets or sets the recognized word text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets the confidence score for this word, when provided by the OCR backend.
    /// </summary>
    [JsonPropertyName("confidence")]
    public float? Confidence { get; set; }

    /// <summary>
    /// Gets or sets the X coordinate (pixels) of the top-left corner of the word bounding box.
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate (pixels) of the top-left corner of the word bounding box.
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the width (pixels) of the word bounding box.
    /// </summary>
    [JsonPropertyName("w")]
    public int W { get; set; }

    /// <summary>
    /// Gets or sets the height (pixels) of the word bounding box.
    /// </summary>
    [JsonPropertyName("h")]
    public int H { get; set; }
}

/// <summary>
/// Provides OCR-engine-specific options that influence recognition behavior.
/// </summary>
public sealed class OcrEngineOptions
{
    /// <summary>
    /// Gets or sets the OCR language code(s) understood by the underlying OCR engine (e.g., <c>eng</c>).
    /// </summary>
    /// <remarks>
    /// For engines that support multiple languages, this may accept a combined language string (implementation-specific),
    /// such as <c>eng+spa</c>.
    /// </remarks>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "eng";
}
