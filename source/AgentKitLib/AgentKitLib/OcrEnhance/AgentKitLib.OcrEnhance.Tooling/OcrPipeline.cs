using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Tooling;

public sealed class OcrPipeline
{
    private readonly IOcrRunOptionsPlanner _optionsPlanner;
    private readonly OcrExtractionService _extract;

    public OcrPipeline(IOcrRunOptionsPlanner optionsPlanner, OcrExtractionService extract)
    {
        _optionsPlanner = optionsPlanner ?? throw new ArgumentNullException(nameof(optionsPlanner));
        _extract = extract ?? throw new ArgumentNullException(nameof(extract));
    }

    public async Task<IReadOnlyList<OcrExtractionArtifact>> RunAsync(
        string imageReference,
        string prompt,
        CancellationToken ct)
    {
        var opts = await _optionsPlanner.CreateOptionsAsync(prompt, ct);

        var ocrOptions = new OcrEngineOptions { Language = opts.Language };

        if (!opts.RunEnhancement)
        {
            return await _extract.ExtractOnlyAsync(
                imageReference: imageReference,
                ocrOptions: ocrOptions,
                saveTxt: opts.SaveTxt,
                saveJson: opts.SaveJson,
                ct: ct);
        }

        return await _extract.EnhanceAndExtractAsync(
            imageReference: imageReference,
            prompt: prompt,
            ocrOptions: ocrOptions,
            includeOriginal: opts.IncludeOriginal,
            saveTxt: opts.SaveTxt,
            saveJson: opts.SaveJson,
            ct: ct);
    }
}