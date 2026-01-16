using System;
using System.IO;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;
using Tesseract;

namespace AgentKitLib.OcrEnhance.Ocr.Tesseract;

/// <summary>
/// Tesseract-based implementation of <see cref="IOcrEngine"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation runs OCR locally using the Tesseract engine and returns a normalized <see cref="OcrResult"/>
/// (full text, mean confidence, and word-level bounding boxes when available).
/// </para>
/// <para>
/// Tesseract requires a <c>tessdata</c> directory that contains language training files such as
/// <c>eng.traineddata</c>. The configured <paramref name="dataPath"/> may point either directly to the
/// <c>tessdata</c> directory or to a parent directory containing a <c>tessdata</c> child.
/// </para>
/// <para>
/// Note: This class creates a new <see cref="TesseractEngine"/> per call. If you need higher throughput,
/// consider pooling/reusing engines carefully (Tesseract engine instances are not guaranteed to be thread-safe).
/// </para>
/// </remarks>
public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly string _dataPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TesseractOcrEngine"/> class.
    /// </summary>
    /// <param name="dataPath">
    /// Path to the Tesseract language data directory (the folder containing training files like
    /// <c>eng.traineddata</c>), or a parent folder containing a <c>tessdata</c> child directory.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataPath"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the resolved <c>tessdata</c> directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown when required training data (e.g., <c>eng.traineddata</c>) is missing.</exception>
    public TesseractOcrEngine(string dataPath)
    {
        if (string.IsNullOrWhiteSpace(dataPath))
            throw new ArgumentNullException(nameof(dataPath));

        _dataPath = ResolveTessdataPath(dataPath);

        if (!Directory.Exists(_dataPath))
            throw new DirectoryNotFoundException($"Tesseract tessdata directory not found: '{_dataPath}'.");

        // Fail fast with a clear message in the common case where tessdata is configured incorrectly.
        // (This library defaults to 'eng', so 'eng.traineddata' is required unless the caller changes language.)
        var engFile = Path.Combine(_dataPath, "eng.traineddata");
        if (!File.Exists(engFile))
        {
            throw new FileNotFoundException(
                $"Missing training data file: '{engFile}'. " +
                "Set OcrEnhance:Tesseract:DataPath to the folder that contains 'eng.traineddata' (the 'tessdata' folder).");
        }
    }

    /// <summary>
    /// Runs OCR on the provided image stream using Tesseract and returns a normalized result.
    /// </summary>
    /// <param name="image">
    /// The input image stream. The stream is owned by the caller and must not be disposed by this method.
    /// </param>
    /// <param name="options">
    /// OCR options (e.g., language). For Tesseract, this maps to <c>*.traineddata</c> files in the tessdata directory.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="OcrResult"/> containing recognized text and metadata.</returns>
    /// <remarks>
    /// Tesseract consumes a <see cref="Pix"/> image. This implementation buffers the stream into memory and
    /// uses <see cref="Pix.LoadFromMemory(byte[])"/> to avoid file I/O and minimize file-locking concerns.
    /// </remarks>
    public Task<OcrResult> ReadAsync(Stream image, OcrEngineOptions options, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Buffer the image bytes so we can feed Tesseract via Pix without requiring a seekable stream.
        using var ms = new MemoryStream();
        image.CopyTo(ms);
        var bytes = ms.ToArray();

        // Create an engine for the requested language and process the image.
        using var engine = new TesseractEngine(_dataPath, options.Language, EngineMode.Default);

        using var pix = Pix.LoadFromMemory(bytes);
        using var page = engine.Process(pix);

        var result = new OcrResult
        {
            Engine = "tesseract",
            Text = page.GetText() ?? "",
            MeanConfidence = page.GetMeanConfidence()
        };

        // Extract word-level tokens + bounding boxes when available.
        using (var iter = page.GetIterator())
        {
            iter.Begin();
            do
            {
                ct.ThrowIfCancellationRequested();

                // The API iterates over elements at different levels; we only capture word granularity.
                if (!iter.IsAtBeginningOf(PageIteratorLevel.Word))
                    continue;

                var word = iter.GetText(PageIteratorLevel.Word);
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (!iter.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                    continue;

                float? conf = null;
                try
                {
                    // Some builds/wrappers may throw for confidence; treat as optional.
                    conf = iter.GetConfidence(PageIteratorLevel.Word);
                }
                catch
                {
                    conf = null;
                }

                result.Words.Add(new OcrWord
                {
                    Text = word.Trim(),
                    Confidence = conf,
                    X = rect.X1,
                    Y = rect.Y1,
                    W = rect.Width,
                    H = rect.Height
                });
            } while (iter.Next(PageIteratorLevel.Word));
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Resolves the actual tessdata directory from a configured path.
    /// </summary>
    /// <param name="configuredPath">
    /// Either a direct <c>tessdata</c> directory (containing <c>eng.traineddata</c>) or a parent directory
    /// containing a <c>tessdata</c> child.
    /// </param>
    /// <returns>The resolved directory path to use for <see cref="TesseractEngine"/>.</returns>
    private static string ResolveTessdataPath(string configuredPath)
    {
        // Accept either:
        // - ".../tessdata" (contains eng.traineddata)
        // - ".../" where ".../tessdata" contains eng.traineddata
        var full = Path.GetFullPath(configuredPath);

        if (File.Exists(Path.Combine(full, "eng.traineddata")))
            return full;

        var child = Path.Combine(full, "tessdata");
        if (File.Exists(Path.Combine(child, "eng.traineddata")))
            return child;

        // Fall back to the configured path to keep error messages intuitive.
        return full;
    }
}
