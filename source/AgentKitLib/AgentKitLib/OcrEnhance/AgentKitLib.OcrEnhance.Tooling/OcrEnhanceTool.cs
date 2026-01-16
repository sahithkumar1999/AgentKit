using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using AgentKitLib.OcrEnhance.Core.Abstractions;

namespace AgentKitLib.OcrEnhance.Tooling;

public sealed class OcrEnhanceTool
{
    private readonly IEnhancementService _svc;

    public OcrEnhanceTool(IEnhancementService svc)
    {
        _svc = svc;
    }

    [Description("Generates enhanced image variants to improve OCR based on a natural language prompt. Returns the comma-separated list of references.")]
    public async Task<string> EnhanceForOcr(
        [Description("The reference number of the image, e.g., 123ABC.")] string imageReference,
        [Description("Natural language prompt describing improvements and/or ranges.")] string prompt)
    {
        var refs = await _svc.EnhanceForOcrAsync(imageReference, prompt, CancellationToken.None);
        return string.Join(",", refs);
    }
}
