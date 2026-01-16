using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Tooling;

public sealed class OcrExtractionService
{
    private readonly IImageStore _store;
    private readonly IEnhancementService _enhancement;
    private readonly IOcrEngine _ocr;
    private readonly string _storageRoot;

    public OcrExtractionService(
        IImageStore store,
        IEnhancementService enhancement,
        IOcrEngine ocr,
        string storageRoot)
    {
        _store = store;
        _enhancement = enhancement;
        _ocr = ocr;
        _storageRoot = storageRoot;
    }

    public async Task<IReadOnlyList<OcrExtractionArtifact>> ExtractOnlyAsync(
        string imageReference,
        OcrEngineOptions ocrOptions,
        bool saveTxt,
        bool saveJson,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await using var img = await _store.OpenReadAsync(imageReference, ct);

        var sw = Stopwatch.StartNew();
        OcrResult ocrResult = await _ocr.ReadAsync(img, ocrOptions, ct);
        sw.Stop();

        var artifact = new OcrExtractionArtifact
        {
            ImageReference = imageReference,
            BaseReference = imageReference,
            Prompt = "OCR_ONLY",
            Ms = sw.ElapsedMilliseconds,
            Result = ocrResult
        };

        if (saveTxt)
        {
            artifact.TxtPath = Path.Combine(_storageRoot, $"{imageReference}.ocr.txt");
            await File.WriteAllTextAsync(artifact.TxtPath, ocrResult.Text ?? "", Encoding.UTF8, ct);
        }

        if (saveJson)
        {
            artifact.JsonPath = Path.Combine(_storageRoot, $"{imageReference}.ocr.json");
            var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(artifact.JsonPath, json, Encoding.UTF8, ct);
        }

        return new[] { artifact };
    }

    public async Task<IReadOnlyList<OcrExtractionArtifact>> EnhanceAndExtractAsync(
        string imageReference,
        string prompt,
        OcrEngineOptions ocrOptions,
        bool includeOriginal,
        bool saveTxt,
        bool saveJson,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var refs = (await _enhancement.EnhanceForOcrAsync(imageReference, prompt, ct)).ToList();
        if (includeOriginal && !refs.Contains(imageReference, StringComparer.OrdinalIgnoreCase))
            refs.Insert(0, imageReference);

        var artifacts = new List<OcrExtractionArtifact>();
        foreach (var r in refs)
        {
            ct.ThrowIfCancellationRequested();

            await using var img = await _store.OpenReadAsync(r, ct);

            var sw = Stopwatch.StartNew();
            OcrResult ocrResult = await _ocr.ReadAsync(img, ocrOptions, ct);
            sw.Stop();

            var artifact = new OcrExtractionArtifact
            {
                ImageReference = r,
                BaseReference = imageReference,
                Prompt = prompt,
                Ms = sw.ElapsedMilliseconds,
                Result = ocrResult
            };

            if (saveTxt)
            {
                artifact.TxtPath = Path.Combine(_storageRoot, $"{r}.ocr.txt");
                await File.WriteAllTextAsync(artifact.TxtPath, ocrResult.Text ?? "", Encoding.UTF8, ct);
            }

            if (saveJson)
            {
                artifact.JsonPath = Path.Combine(_storageRoot, $"{r}.ocr.json");
                var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(artifact.JsonPath, json, Encoding.UTF8, ct);
            }

            artifacts.Add(artifact);
        }

        return artifacts;
    }
}

public sealed class OcrExtractionArtifact
{
    public string ImageReference { get; set; } = "";
    public string BaseReference { get; set; } = "";
    public string Prompt { get; set; } = "";
    public long Ms { get; set; }
    public OcrResult Result { get; set; } = new();

    public string? TxtPath { get; set; }
    public string? JsonPath { get; set; }
}
