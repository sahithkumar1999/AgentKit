using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines a planner that derives execution options for an OCR run from a natural-language prompt.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction allows the application layer (e.g., console app) to remain thin by delegating
/// decisions such as:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>Whether to run image enhancement prior to OCR.</description>
///   </item>
///   <item>
///     <description>Which output artifacts to produce (e.g., save text, save JSON, or produce no files).</description>
///   </item>
///   <item>
///     <description>Which language model to use (e.g., <c>eng</c>).</description>
///   </item>
/// </list>
/// <para>
/// Implementations may be deterministic (rule-based), model-driven (e.g., OpenAI), or a hybrid of both.
/// </para>
/// </remarks>
public interface IOcrRunOptionsPlanner
{
    /// <summary>
    /// Creates an <see cref="OcrRunOptions"/> instance from the provided natural-language prompt.
    /// </summary>
    /// <param name="prompt">
    /// The user prompt describing the desired OCR behavior (e.g., "Return ONLY JSON", "OCR only, no enhancement",
    /// "Create 3 variants then OCR").
    /// </param>
    /// <param name="ct">
    /// A cancellation token for aborting the planning step.
    /// </param>
    /// <returns>
    /// A task whose result contains the planned <see cref="OcrRunOptions"/> to apply when executing the OCR pipeline.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    Task<OcrRunOptions> CreateOptionsAsync(string prompt, CancellationToken ct);
}
