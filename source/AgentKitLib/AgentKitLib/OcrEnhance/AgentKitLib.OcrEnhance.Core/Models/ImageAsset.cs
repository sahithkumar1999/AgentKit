using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Core.Models;

/// <summary>
/// Represents a persisted image asset stored by an <see cref="Abstractions.IImageStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// This model is intended to capture the core metadata required to identify, retrieve, and audit an image
/// (original or variant) within a storage backend.
/// </para>
/// <para>
/// The <see cref="Reference"/> value is the primary identifier used throughout the OCR enhancement and extraction
/// pipeline (e.g., passed into services and used to open the image stream later).
/// </para>
/// <para>
/// Values such as <see cref="Width"/> and <see cref="Height"/> may be unavailable depending on the storage backend
/// or whether metadata extraction is performed at save time.
/// </para>
/// </remarks>
/// <param name="Reference">Storage reference (key/identifier) used to retrieve the image.</param>
/// <param name="FileName">Original file name (or best-known name) associated with the stored image.</param>
/// <param name="ContentType">MIME content type (e.g., <c>image/png</c>, <c>image/jpeg</c>).</param>
/// <param name="SizeBytes">Size of the stored image in bytes.</param>
/// <param name="Width">Image width in pixels, when known.</param>
/// <param name="Height">Image height in pixels, when known.</param>
/// <param name="CreatedUtc">UTC timestamp indicating when the asset was created/stored.</param>
public sealed record ImageAsset(
    string Reference,
    string FileName,
    string ContentType,
    long SizeBytes,
    int? Width,
    int? Height,
    DateTimeOffset CreatedUtc
);

