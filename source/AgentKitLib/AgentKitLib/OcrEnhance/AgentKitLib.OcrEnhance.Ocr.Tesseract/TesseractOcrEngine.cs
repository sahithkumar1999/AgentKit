using System;
using System.Collections.Generic;
using System.IO;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;
using Tesseract;

namespace AgentKitLib.OcrEnhance.Ocr.Tesseract;

public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly string _dataPath;

    public TesseractOcrEngine(string dataPath)
    {
        if (string.IsNullOrWhiteSpace(dataPath))
            throw new ArgumentNullException(nameof(dataPath));

        _dataPath = ResolveTessdataPath(dataPath);

        if (!Directory.Exists(_dataPath))
            throw new DirectoryNotFoundException($"Tesseract tessdata directory not found: '{_dataPath}'.");

        // Prefer failing fast with a clear message.
        var engFile = Path.Combine(_dataPath, "eng.traineddata");
        if (!File.Exists(engFile))
        {
            throw new FileNotFoundException(
                $"Missing training data file: '{engFile}'. " +
                "Set OcrEnhance:Tesseract:DataPath to the folder that contains 'eng.traineddata' (the 'tessdata' folder).");
        }
    }

    public Task<OcrResult> ReadAsync(Stream image, OcrEngineOptions options, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var ms = new MemoryStream();
        image.CopyTo(ms);
        var bytes = ms.ToArray();

        using var engine = new TesseractEngine(_dataPath, options.Language, EngineMode.Default);

        using var pix = Pix.LoadFromMemory(bytes);
        using var page = engine.Process(pix);

        var result = new OcrResult
        {
            Engine = "tesseract",
            Text = page.GetText() ?? "",
            MeanConfidence = page.GetMeanConfidence()
        };

        using (var iter = page.GetIterator())
        {
            iter.Begin();
            do
            {
                ct.ThrowIfCancellationRequested();

                if (!iter.IsAtBeginningOf(PageIteratorLevel.Word))
                    continue;

                var word = iter.GetText(PageIteratorLevel.Word);
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (!iter.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                    continue;

                float? conf = null;
                try { conf = iter.GetConfidence(PageIteratorLevel.Word); } catch { }

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
