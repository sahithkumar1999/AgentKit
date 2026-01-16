using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Planning.OpenAI;

public sealed class OpenAiPromptPlanner : IPromptPlanner
{
    private readonly HttpClient _http;
    private readonly OpenAiPlannerOptions _opts;

    public OpenAiPromptPlanner(HttpClient http, OpenAiPlannerOptions opts)
    {
        _http = http;
        _opts = opts;
    }

    public async Task<EnhancementPlan> CreatePlanAsync(string userPrompt, CancellationToken ct)
    {
        // IMPORTANT: This is a minimal skeleton payload.
        // In production, use strict JSON schema + server-side validation.
        var system = """
You are a planner for an OCR image enhancement tool.
Return ONLY valid JSON with shape:
{
  "variants": [
    { "name": "short_name", "steps": [ { "op": "operation", "params": { ... } } ] }
  ]
}
Operations allowed: zoom, rotate, autocontrast, clahe, denoise, binarize, deskew, sharpen, brightness, gamma.
Rules:
- If prompt requests ranges (start/end/step), generate multiple variants accordingly.
- Never exceed the end bound (no overshoot).
- Keep variant names short and unique.
""";

        var payload = new
        {
            model = _opts.Model,
            input = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = userPrompt }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _opts.Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var rawResponseJson = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI planning call failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{rawResponseJson}");

        // 1) Extract assistant text from the Responses API JSON
        var text = ExtractTextFromResponsesApi(rawResponseJson);

        // 2) Clean markdown code fences (```json ... ```
        text = StripMarkdownCodeFences(text);

        // 3) Extract the JSON object from the text (guards against accidental leading/trailing text)
        var planJson = ExtractFirstJsonObject(text);

        var plan = JsonSerializer.Deserialize<EnhancementPlan>(
            planJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (plan is null || plan.Variants.Count == 0)
            throw new InvalidOperationException($"Planner returned no variants. Extracted JSON:\n{planJson}");

        return plan;
    }

    private static string ExtractTextFromResponsesApi(string rawResponseJson)
    {
        using var doc = JsonDocument.Parse(rawResponseJson);

        // Typical Responses API shape:
        // { "output": [ { "content": [ { "type": "output_text", "text": "..." } ] } ] }
        if (!doc.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Unexpected Responses API shape: missing 'output' array.");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var c in content.EnumerateArray())
            {
                if (c.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                {
                    var text = textEl.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text!;
                }
            }
        }

        throw new InvalidOperationException("Could not find 'text' content in Responses API response.");
    }

    private static string StripMarkdownCodeFences(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return s;

        // Common model outputs:
        // ```json
        // { ... }
        // ```
        s = s.Trim();

        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            // remove starting fence line (``` or ```json)
            var firstNewLine = s.IndexOf('\n');
            if (firstNewLine >= 0)
                s = s[(firstNewLine + 1)..];

            // remove ending fence
            var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
                s = s[..lastFence];

            s = s.Trim();
        }

        return s;
    }

    private static string ExtractFirstJsonObject(string raw)
    {
        // Very small helper. Replace with robust parsing in production.
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException($"Did not find a JSON object in planner output:\n{raw}");

        return raw.Substring(start, end - start + 1);
    }
}
