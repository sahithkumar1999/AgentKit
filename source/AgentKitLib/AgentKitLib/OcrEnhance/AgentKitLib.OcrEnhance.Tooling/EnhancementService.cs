using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Tooling;

public sealed class EnhancementService : IEnhancementService
{
    private readonly IImageStore _store;
    private readonly IPromptPlanner _planner;
    private readonly IImageProcessor _processor;

    public EnhancementService(IImageStore store, IPromptPlanner planner, IImageProcessor processor)
    {
        _store = store;
        _planner = planner;
        _processor = processor;
    }

    public async Task<IReadOnlyList<string>> EnhanceForOcrAsync(string imageReference, string prompt, CancellationToken ct)
    {
        if (!await _store.ExistsAsync(imageReference, ct))
            throw new FileNotFoundException($"Unknown image reference: {imageReference}");

        EnhancementPlan plan = await _planner.CreatePlanAsync(prompt, ct);

        var results = new List<string>();

        // Load base image once
        await using var baseStream = await _store.OpenReadAsync(imageReference, ct);

        // For each variant, apply steps
        int idx = 0;
        foreach (var variant in plan.Variants)
        {
            idx++;
            baseStream.Position = 0;

            await using var output = await _processor.ApplyAsync(baseStream, variant.Steps, ct);

            var suffix = $"v{idx:000}_{Sanitize(variant.Name)}";
            var savedRef = await _store.SaveVariantAsync(output, imageReference, suffix, ".png", ct);
            results.Add(savedRef);
        }

        return results;
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "variant";
        var chars = s.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-').ToArray();
        return new string(chars).Trim('_', '-') switch { "" => "variant", var x => x };
    }
}

