using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines a high-level orchestration service that enhances an existing image for OCR based on a user prompt.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction represents the "front door" API for the OCR enhancement workflow. A typical implementation
/// coordinates multiple lower-level components, such as:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// An image store to open the original image and persist outputs (original + generated variants).
/// </description>
/// </item>
/// <item>
/// <description>
/// A prompt planner to translate the natural-language <paramref name="prompt"/> into an ordered set of image
/// processing steps (often via an <c>EnhancementPlan</c> containing one or more variants).
/// </description>
/// </item>
/// <item>
/// <description>
/// An image processor to execute the plan steps and produce processed image outputs.
/// </description>
/// </item>
/// </list>
/// <para>
/// The result of this call is a set of image references (storage keys) that the caller can later retrieve
/// for OCR, preview, or auditing.
/// </para>
/// <para>
/// This interface returns references instead of image bytes/streams to keep the API storage-friendly and
/// to avoid transferring large payloads through the service boundary.
/// </para>
/// </remarks>
public interface IEnhancementService
{
    /// <summary>
    /// Enhances a previously stored image for OCR based on the given prompt and returns references
    /// to the generated images.
    /// </summary>
    /// <param name="imageReference">
    /// The reference (storage key / identifier) of the original image to enhance. This value is expected
    /// to point to an existing image in the configured storage system.
    /// </param>
    /// <param name="prompt">
    /// A natural-language prompt describing the desired enhancement goals (e.g., "deskew and sharpen",
    /// "improve faint text", "reduce noise and increase contrast").
    /// </param>
    /// <param name="ct">
    /// Cancellation token used to cancel the operation. Implementations should honor cancellation throughout:
    /// reading the input image, planning steps, processing variants, and saving results.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous enhancement operation. The task result is a read-only list of
    /// image references corresponding to the generated outputs.
    /// </returns>
    /// <remarks>
    /// Output list conventions are implementation-defined, but common patterns include:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Returning one reference per generated variant (e.g., one per plan variant).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Optionally including the original <paramref name="imageReference"/> in the returned list (for convenience),
    /// typically as the first element.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Returning references in the same order as the planned variants were executed.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// Implementation guidance:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Validate that <paramref name="imageReference"/> exists before processing; return a clear error if not.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Ensure generated references are unique/stable and are grouped/related to the base image where appropriate.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Consider idempotency: if the same image + prompt produces the same variants, you may choose to reuse
    /// existing outputs rather than regenerating them.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// May be thrown by implementations when <paramref name="imageReference"/> or <paramref name="prompt"/> is
    /// null/empty/whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    // Returns list of generated image references (including original if you want)
    Task<IReadOnlyList<string>> EnhanceForOcrAsync(string imageReference, string prompt, CancellationToken ct);
}
