using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines a component responsible for converting a natural-language prompt into an
/// <see cref="EnhancementPlan"/> that can be executed by the OCR enhancement pipeline.
/// </summary>
/// <remarks>
/// <para>
/// A prompt planner sits at the boundary between user intent (free-form text) and a structured,
/// machine-executable enhancement pipeline. Implementations typically:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Parse the <paramref name="userPrompt"/> for constraints and goals (e.g., "improve faint text",
/// "fix rotation", "reduce noise").
/// </description>
/// </item>
/// <item>
/// <description>
/// Choose one or more variants (A/B strategies) and assemble a step-by-step plan (operations + parameters).
/// </description>
/// </item>
/// <item>
/// <description>
/// Optionally use heuristics, rules, or an LLM to map intent to supported operations.
/// </description>
/// </item>
/// </list>
/// <para>
/// The returned <see cref="EnhancementPlan"/> should be valid for the downstream processor:
/// operations should be known, parameters should be compatible, and steps should be ordered correctly
/// (e.g., deskew/rotate before binarization in many pipelines).
/// </para>
/// </remarks>
public interface IPromptPlanner
{
    /// <summary>
    /// Creates an <see cref="EnhancementPlan"/> from a user-provided natural-language prompt.
    /// </summary>
    /// <param name="userPrompt">
    /// The user prompt describing the desired enhancements (e.g., "deskew and sharpen",
    /// "increase contrast and remove noise", "make this receipt easier to read").
    /// </param>
    /// <param name="ct">
    /// Cancellation token used to cancel the planning operation. This is especially important for
    /// implementations that may call external services (e.g., LLM endpoints) or perform non-trivial analysis.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous planning operation. The result is an <see cref="EnhancementPlan"/>
    /// containing one or more variants with ordered steps suitable for execution by an enhancer/processor.
    /// </returns>
    /// <remarks>
    /// Implementation guidance:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Prefer deterministic output for identical prompts when possible (improves testability and caching).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Validate and normalize the plan (known operations, required parameters present, safe defaults).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Consider returning multiple variants when the prompt is ambiguous (e.g., one variant uses CLAHE,
    /// another uses autocontrast + binarize).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// May be thrown by implementations when <paramref name="userPrompt"/> is null/empty/whitespace or invalid.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    Task<EnhancementPlan> CreatePlanAsync(string userPrompt, CancellationToken ct);
}
