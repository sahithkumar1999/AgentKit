using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines an OCR engine capable of extracting text (and optionally structured data like word boxes)
/// from an image stream.
/// </summary>
/// <remarks>
/// <para>
/// This interface is intentionally storage- and format-agnostic: callers provide an image as a <see cref="Stream"/>
/// and receive a normalized <see cref="OcrResult"/>.
/// </para>
/// <para>
/// Implementations may wrap different OCR backends (e.g., Tesseract, cloud OCR services, custom ML models).
/// </para>
/// <para>
/// Implementations should assume the input stream can be non-seekable; callers are not required to reset
/// the stream position. Implementations should also avoid disposing the input stream.
/// </para>
/// </remarks>
public interface IOcrEngine
{
    /// <summary>
    /// Runs OCR on the provided image and returns extracted text and related OCR metadata.
    /// </summary>
    /// <param name="image">
    /// The input image stream. The stream is owned by the caller and must not be disposed by the implementation.
    /// The stream may or may not support seeking.
    /// </param>
    /// <param name="options">
    /// Options controlling OCR execution (e.g., language).
    /// </param>
    /// <param name="ct">
    /// A cancellation token for aborting OCR execution.
    /// </param>
    /// <returns>
    /// A task whose result contains the extracted <see cref="OcrResult"/> including recognized text and
    /// any available structured detail (e.g., word boxes and confidence).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="image"/> or <paramref name="options"/> is null (implementation-specific).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    Task<OcrResult> ReadAsync(Stream image, OcrEngineOptions options, CancellationToken ct);
}
