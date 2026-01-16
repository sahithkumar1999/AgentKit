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

        string model =
            config["OcrEnhance:OpenAI:Model"]
            ?? throw new InvalidOperationException("Missing config: OcrEnhance:OpenAI:Model");

        string apiKeyEnvVar =
            config["OcrEnhance:OpenAI:ApiKeyEnvVar"]
            ?? "OPENAI_API_KEY";

        string tesseractDataPath =
            config["OcrEnhance:Tesseract:DataPath"]
            ?? throw new InvalidOperationException("Missing config: OcrEnhance:Tesseract:DataPath");

        string? apiKey = Environment.GetEnvironmentVariable(apiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Environment variable '{apiKeyEnvVar}' is not set.");

        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        string imagePathArg = args[0];
        string prompt = args[1];

        string imagePath = ResolveExistingPath(imagePathArg)
            ?? throw new FileNotFoundException($"Input image not found: {imagePathArg}");

        var store = new LocalImageStore(new LocalImageStoreOptions { RootDirectory = storageRoot });

        string ext = Path.GetExtension(imagePath);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".png";

        await using var fileStream = File.OpenRead(imagePath);

        string imageReference = await store.SaveAsync(
            imageStream: fileStream,
            fileExtension: ext,
            suggestedReference: Path.GetFileNameWithoutExtension(imagePath),
            ct: cts.Token);

        var pipeline = ToolFactory.CreateDefaultOcrPipeline(
            storageRoot: storageRoot,
            openAiApiKey: apiKey,
            endpoint: endpoint,
            tesseractDataPath: tesseractDataPath,
            model: model);

        var artifacts = await pipeline.RunAsync(imageReference, prompt, cts.Token);

        Console.WriteLine("OCR outputs:");
        foreach (var a in artifacts)
        {
            Console.WriteLine($"  {a.ImageReference}");
            Console.WriteLine($"    txt:  {a.TxtPath}");
            Console.WriteLine($"    json: {a.JsonPath}");
            Console.WriteLine($"    conf: {a.Result.MeanConfidence:0.000}  ms: {a.Ms}");
        }
    }

    private static string? ResolveExistingPath(string path)
    {
        if (File.Exists(path))
            return Path.GetFullPath(path);

        var baseDirCandidate = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(baseDirCandidate))
            return Path.GetFullPath(baseDirCandidate);

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
    }
}
