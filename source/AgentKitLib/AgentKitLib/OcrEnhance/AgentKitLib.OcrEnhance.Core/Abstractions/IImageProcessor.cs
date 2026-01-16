using AgentKitLib.OcrEnhance.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines the contract for a component that applies an ordered sequence of image enhancement operations
/// (represented as <see cref="PlanStep"/> items) to an input image.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction is the execution counterpart to planning (see <c>EnhancementPlan</c> / <c>PlanVariant</c>).
/// Given an input image and a list of steps, an implementation is expected to:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Decode the input stream into an in-memory image representation (implementation-specific).
/// </description>
/// </item>
/// <item>
/// <description>
/// Apply each step in the provided order (pipeline semantics).
/// </description>
/// </item>
/// <item>
/// <description>
/// Encode the resulting image to an output stream and return it to the caller.
/// </description>
/// </item>
/// </list>
/// <para>
/// Typical operations include (depending on your runtime implementation): deskew, rotate, resize/zoom,
/// contrast adjustments (autocontrast/CLAHE), denoise, sharpening, and binarization.
/// </para>
/// <para>
/// This interface intentionally does not prescribe the underlying imaging library (e.g., ImageSharp, OpenCV),
/// the output format, or how parameters are validated—those are responsibilities of the implementation.
/// </para>
/// </remarks>
public interface IImageProcessor
{
    /// <summary>
    /// Applies the provided enhancement steps to the supplied input image stream and returns the enhanced image.
    /// </summary>
    /// <param name="inputImage">
    /// The input image stream to process. The stream should generally be readable and positioned at the start
    /// of the image content. Implementations should read from this stream but should not dispose it unless
    /// explicitly documented by the implementation.
    /// </param>
    /// <param name="steps">
    /// The ordered list of enhancement steps to apply. Steps must be applied sequentially in the order provided.
    /// Each <see cref="PlanStep"/> contains:
    /// <list type="bullet">
    /// <item><description><c>Op</c>: the operation identifier (e.g., <c>"deskew"</c>, <c>"sharpen"</c>).</description></item>
    /// <item><description><c>Params</c>: an operation-specific parameter bag (e.g., <c>{"strength": 1.2}</c>).</description></item>
    /// </list>
    /// </param>
    /// <param name="ct">
    /// Cancellation token to observe. Implementations should honor cancellation between steps and during
    /// any long-running operations (decoding, processing, encoding, I/O).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation. The task result is a <see cref="Stream"/>
    /// containing the processed image. The returned stream should be positioned at the beginning so callers can
    /// immediately read it. The caller is responsible for disposing the returned stream.
    /// </returns>
    /// <remarks>
    /// Implementation guidance:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Validate inputs (non-null stream, non-null steps, supported operations, required parameters).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Prefer predictable output encoding (e.g., always PNG) or clearly document the chosen output format.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Avoid mutating caller-owned streams in unexpected ways (e.g., do not close <paramref name="inputImage"/>).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Consider returning a non-seekable stream only if necessary; seekable output can be convenient for callers.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// May be thrown by implementations when <paramref name="inputImage"/> or <paramref name="steps"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// May be thrown by implementations when steps contain unsupported operations or invalid parameters.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    // Applies the ordered steps to a given input image stream.
    Task<Stream> ApplyAsync(Stream inputImage, IReadOnlyList<PlanStep> steps, CancellationToken ct);
}
