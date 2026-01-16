# AgentKit.OcrEnhance

## Overview

AgentKit.OcrEnhance is a component of the AgentKit framework designed to enhance images for improved Optical Character Recognition (OCR) results. This module provides preprocessing and enhancement capabilities to optimize images before OCR processing.

## Project Structure

The OcrEnhance component is organized into the following structure:

- **AgentKit.OcrEnhance.Core**: Core models and business logic for image enhancement

## Core Models

### ImageAsset

Represents an image asset with comprehensive metadata for processing and tracking.

**Properties:**
- `Reference` (string): Unique reference identifier for the image asset
- `FileName` (string): Original filename of the image
- `ContentType` (string): MIME type of the image (e.g., image/jpeg, image/png)
- `SizeBytes` (long): Size of the image file in bytes
- `Width` (int?): Width of the image in pixels (optional)
- `Height` (int?): Height of the image in pixels (optional)
- `CreatedUtc` (DateTimeOffset): UTC timestamp when the asset was created

## Features

- Image preprocessing for OCR optimization
- Metadata tracking for image assets
- Support for multiple image formats

## Requirements

- .NET 10

## Getting Started

(Coming soon - Add usage examples and setup instructions)

## License

See the main AgentKit repository for license information.

## Contributing

Contributions are welcome! Please refer to the main AgentKit repository for contribution guidelines.

## Repository

[https://github.com/sahithkumar1999/AgentKit](https://github.com/sahithkumar1999/AgentKit)