using System.Text.Json.Serialization;

namespace AgentKitLib.OcrEnhance.Core.Models;

/// <summary>
/// Represents high-level execution options for an OCR run.
/// </summary>
/// <remarks>
/// <para>
/// This model controls orchestration behavior (pipeline decisions) rather than low-level OCR engine behavior.
/// Typical decisions include whether to run image enhancement first, which artifacts to persist, and which
/// language to use for OCR.
/// </para>
/// <para>
/// These options are commonly produced by an <see cref="Abstractions.IOcrRunOptionsPlanner"/> (prompt ? options),
/// and then consumed by an orchestration layer (e.g., <c>OcrPipeline</c>).
/// </para>
/// </remarks>
public sealed class OcrRunOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the pipeline should run image enhancement before OCR.
    /// </summary>
    /// <remarks>
    /// When enabled, the pipeline may generate one or more enhanced variants using the configured enhancement
    /// planner/processor and then perform OCR on those outputs.
    /// </remarks>
    [JsonPropertyName("runEnhancement")]
    public bool RunEnhancement { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the original (unmodified) image should be included in the OCR run
    /// alongside any enhanced variants.
    /// </summary>
    /// <remarks>
    /// When <see cref="RunEnhancement"/> is <see langword="true"/>, enabling this option allows baseline comparison
    /// between the original image and enhanced variants.
    /// </remarks>
    [JsonPropertyName("includeOriginal")]
    public bool IncludeOriginal { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline should persist the OCR result as a plain-text
    /// file (e.g., <c>*.ocr.txt</c>).
    /// </summary>
    /// <remarks>
    /// This affects persistence/output generation only and does not change OCR behavior.
    /// </remarks>
    [JsonPropertyName("saveTxt")]
    public bool SaveTxt { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline should persist the OCR result as a structured JSON
    /// file (e.g., <c>*.ocr.json</c>), including metadata such as confidence and word boxes when available.
    /// </summary>
    /// <remarks>
    /// This affects persistence/output generation only and does not change OCR behavior.
    /// </remarks>
    [JsonPropertyName("saveJson")]
    public bool SaveJson { get; set; } = true;

    /// <summary>
    /// Gets or sets the OCR language code(s) to use (e.g., <c>eng</c>).
    /// </summary>
    /// <remarks>
    /// The accepted values are OCR-engine-specific. For Tesseract, this maps to the language data file(s)
    /// in the configured <c>tessdata</c> directory (e.g., <c>eng.traineddata</c>).
    /// </remarks>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "eng";
}