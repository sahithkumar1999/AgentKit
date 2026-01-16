using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentKitLib.OcrEnhance.Storage.Local;
using AgentKitLib.OcrEnhance.Tooling;
using Microsoft.Extensions.Configuration;

namespace AgentKitConsoleApplication;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        IConfiguration config =
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

        string storageRoot =
            config["OcrEnhance:StorageRoot"]
            ?? throw new InvalidOperationException("Missing config: OcrEnhance:StorageRoot");

        string endpoint =
            config["OcrEnhance:OpenAI:Endpoint"]
            ?? throw new InvalidOperationException("Missing config: OcrEnhance:OpenAI:Endpoint");

        // Require the model to be present in JSON (no silent fallback).
        string model =
            config["OcrEnhance:OpenAI:Model"]
            ?? throw new InvalidOperationException("Missing config: OcrEnhance:OpenAI:Model");

        string apiKeyEnvVar =
            config["OcrEnhance:OpenAI:ApiKeyEnvVar"]
            ?? "OPENAI_API_KEY";

        // 2) Read API key from environment variable (don’t store secrets in JSON)
        string? apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Environment variable '{apiKeyEnvVar}' is not set.");

        // 3) Validate args
        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        string imagePathArg = args[0];
        string prompt = args[1];

        string imagePath = ResolveExistingPath(imagePathArg)
            ?? throw new FileNotFoundException(
                $"Input image not found: {imagePathArg}{Environment.NewLine}" +
                $"Tried:{Environment.NewLine}" +
                $"  - {Path.GetFullPath(imagePathArg)}{Environment.NewLine}" +
                $"  - {Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, imagePathArg))}");

        // 4) Create the tool (planner + processor + store wiring)
        var tool = ToolFactory.CreateDefault(
            storageRoot: storageRoot,
            openAiApiKey: apiKey,
            endpoint: endpoint,
            model: model);

        // 5) Import the image into the store to get an imageReference (required by EnhancementService)
        // NOTE: OcrEnhanceTool currently only exposes EnhanceForOcr(reference, prompt), so we save directly
        // using the same LocalImageStore path configured in ToolFactory.
        var store = new LocalImageStore(new LocalImageStoreOptions { RootDirectory = storageRoot });

        string ext = Path.GetExtension(imagePath);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".png";

        await using var fileStream = File.OpenRead(imagePath);

        // This returns a short reference like "A1B2C3D4"; the image is stored under StorageRoot.
        string imageReference = await store.SaveAsync(
            imageStream: fileStream,
            fileExtension: ext,
            suggestedReference: Path.GetFileNameWithoutExtension(imagePath),
            ct: cts.Token);

        Console.WriteLine("Imported image.");
        Console.WriteLine($"  Input:     {imagePath}");
        Console.WriteLine($"  Storage:   {storageRoot}");
        Console.WriteLine($"  Reference: {imageReference}");
        Console.WriteLine();

        // 6) Run enhancement (returns comma-separated references)
        Console.WriteLine("Planning + enhancing (this calls OpenAI and then processes the image locally)...");
        string refsCsv = await tool.EnhanceForOcr(imageReference, prompt);

        Console.WriteLine();
        Console.WriteLine("Generated variant references:");
        foreach (var r in refsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            Console.WriteLine($"  {r}");
    }

    private static string? ResolveExistingPath(string path)
    {
        // 1) As-is (relative to current working directory, or absolute)
        if (File.Exists(path))
            return Path.GetFullPath(path);

        // 2) Relative to the app base directory (bin\Debug\net10.0\...)
        var baseDirCandidate = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(baseDirCandidate))
            return Path.GetFullPath(baseDirCandidate);

        // 3) Relative to the project directory (useful when running from solution/repo root)
        // AppContext.BaseDirectory is: <project>\bin\Debug\net10.0\
        // So go up 3 levels to: <project>\
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var projectDirCandidate = Path.Combine(projectDir, path);
        if (File.Exists(projectDirCandidate))
            return Path.GetFullPath(projectDirCandidate);

        return null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  AgentKitConsoleApplication <imagePath> \"<prompt>\"");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  dotnet run --project .\\AgentKitConsoleApplication\\AgentKitConsoleApplication.csproj -- \".\\AgentKitConsoleApplication\\InputImages\\Screenshot.png\" \"Deskew, increase contrast, denoise lightly, then sharpen for OCR.\"");
    }
}
