using System;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Imaging.OpenCv;
using AgentKitLib.OcrEnhance.Ocr.Tesseract;
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

    /// <summary>
    /// Creates a default OCR extraction service wiring together:
    /// - local image storage
    /// - OpenAI-based prompt planner
    /// - OpenCV-based image processor
    /// - Tesseract OCR engine
    /// </summary>
    public static OcrExtractionService CreateDefaultOcrExtraction(
        string storageRoot,
        string openAiApiKey,
        string endpoint,
        string tesseractDataPath,
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
        var enhanceSvc = new EnhancementService(store, planner, processor);

        IOcrEngine ocr = new TesseractOcrEngine(tesseractDataPath);

        return new OcrExtractionService(store, enhanceSvc, ocr, storageRoot);
    }

    /// <summary>
    /// Creates a prompt-driven OCR pipeline where OpenAI decides:
    /// - whether to run enhancement
    /// - what artifacts to save (txt/json/none)
    /// </summary>
    public static OcrPipeline CreateDefaultOcrPipeline(
        string storageRoot,
        string openAiApiKey,
        string endpoint,
        string tesseractDataPath,
        string model = "gpt-4.1-mini")
    {
        var store = new LocalImageStore(new LocalImageStoreOptions { RootDirectory = storageRoot });

        var http = new HttpClient();

        var opts = new OpenAiPlannerOptions
        {
            ApiKey = openAiApiKey,
            Endpoint = endpoint,
            Model = model
        };

        var enhancementPlanner = new OpenAiPromptPlanner(http, opts);
        var optionsPlanner = new OpenAiOcrRunOptionsPlanner(http, opts);

        var processor = new OpenCvImageProcessor();
        var enhanceSvc = new EnhancementService(store, enhancementPlanner, processor);

        IOcrEngine ocr = new TesseractOcrEngine(tesseractDataPath);

        var extractSvc = new OcrExtractionService(store, enhanceSvc, ocr, storageRoot);

        return new OcrPipeline(optionsPlanner, extractSvc);
    }
}
