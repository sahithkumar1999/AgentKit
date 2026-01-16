# AgentKit.OcrEnhance

## Overview

AgentKit.OcrEnhance is a component of the AgentKit framework focused on preparing images for better Optical Character Recognition (OCR) results. It provides a small set of core models and abstractions that help you:

- Track image inputs with consistent metadata (`ImageAsset`)
- Define *repeatable* image enhancement pipelines using a JSON-serializable plan format (`EnhancementPlan`)
- Store originals and generated variants in a pluggable way (`IImageStore`)
- Generate stable, content-derived identifiers for images (`IImageReferenceGenerator`)

The goal is to make OCR preprocessing configurable, testable, and independent of any specific storage provider or OCR engine.

## Project Structure

- **`AgentKit.OcrEnhance.Core`**: Core models and abstractions for image enhancement planning and storage

## Core Models

### `ImageAsset`

Represents an image asset with metadata required for processing and tracking.

**Properties:**
- `Reference` (`string`): Unique reference identifier for the image asset (typically the storage key or content-derived ID)
- `FileName` (`string`): Original filename of the image
- `ContentType` (`string`): MIME type of the image (e.g., `image/jpeg`, `image/png`)
- `SizeBytes` (`long`): Size of the image file in bytes
- `Width` (`int?`): Width of the image in pixels (optional)
- `Height` (`int?`): Height of the image in pixels (optional)
- `CreatedUtc` (`DateTimeOffset`): UTC timestamp when the asset was created

### `EnhancementPlan`

Defines an enhancement plan consisting of one or more *variants*. Each variant is an ordered pipeline of enhancement *steps*. This lets you try multiple preprocessing strategies and select the best OCR result later.

**Shape (high level):**
- `variants[]` → `PlanVariant`
  - `name` → stable identifier for the variant (e.g., `standard`, `high-contrast`)
  - `steps[]` → `PlanStep`
    - `op` → operation name (string)
    - `params` → operation-specific parameter bag (key/value)

**Common operations (examples):**
- `zoom`, `rotate`, `autocontrast`, `clahe`, `denoise`, `binarize`, `deskew`, `sharpen`

> Note: the plan is designed to be flexible; the execution engine decides which `op` values are supported and what `params` are expected.

#### Example plan JSON

```
{
  "variants": [
    {
      "name": "standard",
      "steps": [
        { "op": "autocontrast", "params": { "cutoff": 0.01 } },
        { "op": "sharpen", "params": { "amount": 1.5 } }
      ]
    },
    {
      "name": "high-contrast",
      "steps": [
        { "op": "autocontrast", "params": { "cutoff": 0.05 } },
        { "op": "binarize", "params": { "threshold": 128 } }
      ]
    }
  ]
}
```

## Core Abstractions

### `IImageStore`

Storage abstraction used to persist original images and enhancement outputs (variants). Keeps the enhancement pipeline independent from the storage backend (local disk, blob storage, etc.).

**Key members:**
- `SaveAsync(...)` → persist an original image and return a reference
- `OpenReadAsync(...)` → open a read stream for an image reference
- `ExistsAsync(...)` → check whether a reference exists
- `SaveVariantAsync(...)` → persist a generated variant tied to a base image reference

### `IImageReferenceGenerator`

Generates an image reference from raw bytes. Useful when you want stable, deterministic IDs (for example, hash-based content addressing) which enables deduplication and idempotent saves.

**Key member:**
- `CreateReference(ReadOnlySpan<byte> imageBytes)` → returns a stable reference string

## Features

- Configurable enhancement pipelines via `EnhancementPlan`
- Multi-variant strategy support for A/B preprocessing
- Metadata tracking for image assets
- Storage abstraction for originals and generated variants
- Stable reference generation via a pluggable strategy

## Requirements

- .NET 10

## Getting Started

(Coming soon)

Suggested flow (conceptual):
1. Add an `IImageReferenceGenerator` implementation (e.g., SHA-256 hash based).
2. Add an `IImageStore` implementation (local file system or cloud storage).
3. Save an original image → get a reference.
4. Execute one or more `EnhancementPlan` variants.
5. Save generated variant images through `SaveVariantAsync(...)`.

## License

See the main AgentKit repository for license information.

## Contributing

Contributions are welcome! Please refer to the main AgentKit repository for contribution guidelines.

## Repository

<https://github.com/sahithkumar1999/AgentKit>