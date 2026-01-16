# AgentKit.OcrEnhance

Image preprocessing for better OCR—**planned from natural-language prompts** and **executed locally**.

This library turns a prompt like:

> “Deskew, increase contrast, denoise lightly, then sharpen for OCR”

into a JSON enhancement plan (via OpenAI), then applies the plan to your image (via OpenCV), saving one or more enhanced *variants* you can feed into your OCR engine.

---

## What you get

- **Prompt → Plan**: `IPromptPlanner` converts natural language into an `EnhancementPlan` (JSON).
- **Plan → Pixels**: `IImageProcessor` applies the plan steps to an image stream (OpenCV implementation included).
- **Originals + Variants storage**: `IImageStore` persists the base image and generated variants (local disk implementation included).
- **A/B variants**: `EnhancementPlan` supports multiple variants (pipelines) for comparing OCR outcomes.

> This module does **not** run OCR itself. It generates enhanced images meant to improve OCR.

---

## Requirements

- **.NET 10**
- An **OpenAI API key** (read from an environment variable)
- OpenCV via `OpenCvSharp` (already referenced by the project)

---

## Quick start (Console app)

### 1) Configure `appsettings.json`

File: `AgentKitConsoleApplication\appsettings.json`

- `OcrEnhance:StorageRoot` is where images and variants are stored.
- `OcrEnhance:OpenAI:Endpoint` defaults to Responses API: `https://api.openai.com/v1/responses`
- `OcrEnhance:OpenAI:Model` must be set (no silent fallback)
- `OcrEnhance:OpenAI:ApiKeyEnvVar` is the env var that contains your key (default `OPENAI_API_KEY`)
- `OcrEnhance:Tesseract:DataPath` is the folder containing Tesseract language data files (e.g., `eng.traineddata`)

### 2) Set your OpenAI key in an environment variable

PowerShell (current session):
```powershell
$env:OPENAI_API_KEY="sk-..."
```
Or permanently (Windows):
```powershell
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", [EnvironmentVariableTarget]::Machine)
```
Linux / macOS:
```bash
export OPENAI_API_KEY="sk..."
```

### 3) Build and run the console app

Open in IDE or command line:

```bash
# Build
dotnet build

# Run
dotnet run --project AgentKitConsoleApplication
```

### 4) Running the Program with arguments

> All examples below assume the image is imported into `OcrEnhance:StorageRoot` and outputs are generated alongside it.

#### Example A: Extract text (OCR only; no enhancement)
```powershell
dotnet run --project AgentKitConsoleApplication -- "ocr" "C:\path\to\image.jpg"
```

#### Example B: Return ONLY JSON + create 3 enhancement variants
```powershell
dotnet run --project AgentKitConsoleApplication -- "Deskew, increase contrast, denoise lightly, then sharpen for OCR" "C:\path\to\image.jpg" --jsononly --variants 3
```

#### Example C: Return ONLY text + create 3 enhancement variants
```powershell
dotnet run --project AgentKitConsoleApplication -- "ocr" "C:\path\to\image.jpg" --variants 3
```

#### Example D: Rotate the image 180 degrees (enhance + OCR)
```powershell
dotnet run --project AgentKitConsoleApplication -- "rotate 180" "C:\path\to\image.jpg"
```

#### Example E: Run using `dotnet user-secrets` (recommended for development)
```bash
# Initialize user secrets (if not done)
dotnet new3 user-secrets

# Set your OpenAI key
dotnet user-secrets set "OcrEnhance:OpenAI:ApiKeyEnvVar" "sk-..."

# Run the app (no need to pass the API key)
dotnet run --project AgentKitConsoleApplication -- "Deskew, increase contrast, denoise lightly, then sharpen for OCR" "C:\path\to\image.jpg"
```

---

## License

See the main AgentKit repository for license information.

## Contributing

Contributions are welcome! Please refer to the main AgentKit repository for contribution guidelines.

## Repository

<https://github.com/sahithkumar1999/AgentKit>