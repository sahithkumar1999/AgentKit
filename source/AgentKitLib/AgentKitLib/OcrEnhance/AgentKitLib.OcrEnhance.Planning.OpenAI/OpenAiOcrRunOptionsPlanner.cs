using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Planning.OpenAI;

public sealed class OpenAiOcrRunOptionsPlanner : IOcrRunOptionsPlanner
{
    private readonly HttpClient _http;
    private readonly OpenAiPlannerOptions _opts;

    public OpenAiOcrRunOptionsPlanner(HttpClient http, OpenAiPlannerOptions opts)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
    }

    public async Task<OcrRunOptions> CreateOptionsAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new OcrRunOptions();

        // Local deterministic overrides (avoid relying on the model for simple switches)
        var p = prompt.ToLowerInvariant();

        // Defaults: safer for cost/side-effects
        var local = new OcrRunOptions
        {
            RunEnhancement = false,
            IncludeOriginal = true,
            SaveTxt = true,
            SaveJson = true,
            Language = "eng"
        };

        bool saysOnlyJson = p.Contains("only json") || p.Contains("return only json");
        bool saysOnlyText = p.Contains("only text") || p.Contains("return only text");
        bool saysNoFiles = p.Contains("no files") || p.Contains("don't write files") || p.Contains("do not write files");

        if (saysOnlyJson)
        {
            local.SaveJson = true;
            local.SaveTxt = false;
        }
        else if (saysOnlyText)
        {
            local.SaveTxt = true;
            local.SaveJson = false;
        }
        else if (saysNoFiles)
        {
            local.SaveTxt = false;
            local.SaveJson = false;
        }

        bool mentionsVariants = p.Contains("variant") || p.Contains("variants") || p.Contains("create 3");
        bool mentionsEnhanceOps =
            p.Contains("enhance") || p.Contains("improve") || p.Contains("denoise") || p.Contains("sharpen") ||
            p.Contains("deskew") || p.Contains("autocontrast") || p.Contains("clahe") || p.Contains("binarize") ||
            p.Contains("rotate") || p.Contains("gamma") || p.Contains("brightness") || p.Contains("zoom") ||
            p.Contains("contrast");

        bool saysOcrOnly = p.Contains("ocr only") || p.Contains("do not enhance") || p.Contains("don't enhance");

        if (saysOcrOnly)
            local.RunEnhancement = false;
        else if (mentionsVariants || mentionsEnhanceOps)
            local.RunEnhancement = true;

        // If local rules already fully determine behavior, skip OpenAI call.
        // (We only need OpenAI if you later add richer options like language auto-detection, includeOriginal heuristics, etc.)
        if (saysOnlyJson || saysOnlyText || saysNoFiles || saysOcrOnly || mentionsVariants || mentionsEnhanceOps)
            return local;

        // Otherwise fall back to OpenAI (current behavior)
        var system = """
You are a planner for an OCR runner. Return ONLY valid JSON with this exact shape:
{
  "runEnhancement": true|false,
  "includeOriginal": true|false,
  "saveTxt": true|false,
  "saveJson": true|false,
  "language": "eng"
}

Defaults:
- runEnhancement=false
- includeOriginal=true
- saveTxt=true
- saveJson=true
- language="eng"
""";

        var payload = new
        {
            model = _opts.Model,
            input = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = prompt }
            },
            text = new
            {
                format = new { type = "json_object" }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _opts.Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var rawResponseJson = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OpenAI options planning call failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{rawResponseJson}");

        var text = ExtractTextFromResponsesApi(rawResponseJson);
        text = StripMarkdownCodeFences(text);
        var optionsJson = ExtractFirstJsonObject(text);

        var options = JsonSerializer.Deserialize<OcrRunOptions>(
            optionsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return options ?? local;
    }

    private static string ExtractTextFromResponsesApi(string rawResponseJson)
    {
        using var doc = JsonDocument.Parse(rawResponseJson);

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

        s = s.Trim();

        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = s.IndexOf('\n');
            if (firstNewLine >= 0)
                s = s[(firstNewLine + 1)..];

            var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
                s = s[..lastFence];

            s = s.Trim();
        }

        return s;
    }

    private static string ExtractFirstJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start < 0 || end <= start)
            throw new InvalidOperationException($"Did not find a JSON object in planner output:\n{raw}");

        return raw.Substring(start, end - start + 1);
    }
}
