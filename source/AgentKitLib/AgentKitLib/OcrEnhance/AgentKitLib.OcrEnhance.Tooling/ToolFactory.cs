using System;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Imaging.OpenCv;
using AgentKitLib.OcrEnhance.Planning.OpenAI;
using AgentKitLib.OcrEnhance.Storage.Local;

namespace AgentKitLib.OcrEnhance.Tooling;

public static class ToolFactory
{
    /// <summary>
    /// Creates a default OCR enhancement tool wiring together:
    /// - local image storage
    /// - OpenAI-based prompt planner
    /// - OpenCV-based image processor
    /// </summary>
    /// <param name="storageRoot">Root directory used by the local image store.</param>
    /// <param name="openAiApiKey">OpenAI API key (recommended: read from environment variable).</param>
    /// <param name="endpoint">OpenAI endpoint URL (e.g., https://api.openai.com/v1/responses).</param>
    /// <param name="model">OpenAI model name (default: gpt-4.1-mini).</param>
    public static OcrEnhanceTool CreateDefault(
        string storageRoot,
        string openAiApiKey,
        string endpoint,
        string model = "gpt-4.1-mini")
    {
        var store = new LocalImageStore(new LocalImageStoreOptions { RootDirectory = storageRoot });

        var http = new HttpClient();

        var planner = new OpenAiPromptPlanner(http, new OpenAiPlannerOptions
        {
            ApiKey = openAiApiKey,
            Endpoint = endpoint,
            Model = model
        });

        var processor = new OpenCvImageProcessor();
        var svc = new EnhancementService(store, planner, processor);

        return new OcrEnhanceTool(svc);
    }
}
