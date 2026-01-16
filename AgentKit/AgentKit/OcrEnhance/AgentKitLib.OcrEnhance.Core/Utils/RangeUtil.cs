using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Core.Utils;

/// <summary>
/// Utility helpers for building integer ranges used by the OCR enhancement pipeline.
/// </summary>
/// <remarks>
/// This helper is intentionally small and dependency-free. It is typically used to generate parameter
/// sweeps (e.g., trying multiple rotation angles, zoom factors expressed as integers, threshold values, etc.)
/// where callers want a predictable, inclusive list of candidate values.
/// </remarks>
public static class RangeUtil
{
    /// <summary>
    /// Builds an inclusive integer range from <paramref name="start"/> to <paramref name="end"/> using the
    /// provided <paramref name="step"/> size.
    /// </summary>
    /// <param name="start">The first value in the returned range.</param>
    /// <param name="end">
    /// The terminating value for the range. The returned list will include <paramref name="end"/> if the
    /// current value lands on it exactly.
    /// </param>
    /// <param name="step">
    /// The step size between consecutive values. Must not be 0.
    /// <para>
    /// If a negative value is supplied, it will be converted to its absolute value. Direction (ascending vs
    /// descending) is determined by comparing <paramref name="start"/> and <paramref name="end"/>, not by the
    /// sign of <paramref name="step"/>.
    /// </para>
    /// </param>
    /// <returns>
    /// A read-only list of integers representing the inclusive range.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Direction rules:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// If <paramref name="start"/> is less than or equal to <paramref name="end"/>, the range is ascending:
    /// <c>start, start + step, ...</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// If <paramref name="start"/> is greater than <paramref name="end"/>, the range is descending:
    /// <c>start, start - step, ...</c>
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// "Overshoot" rule:
    /// The method stops once the next computed value would pass beyond <paramref name="end"/> (greater than
    /// end for ascending, less than end for descending). This guarantees all returned values remain within
    /// the inclusive bounds.
    /// </para>
    /// <para>
    /// Examples:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>BuildInclusiveRange(0, 10, 5)</c> returns <c>[0, 5, 10]</c></description></item>
    /// <item><description><c>BuildInclusiveRange(0, 10, 6)</c> returns <c>[0, 6]</c> (does not overshoot to 12)</description></item>
    /// <item><description><c>BuildInclusiveRange(10, 0, 5)</c> returns <c>[10, 5, 0]</c></description></item>
    /// <item><description><c>BuildInclusiveRange(10, 0, -5)</c> returns <c>[10, 5, 0]</c> (negative step is normalized)</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is 0.
    /// </exception>
    public static IReadOnlyList<int> BuildInclusiveRange(int start, int end, int step)
    {
        // A step size of 0 would cause an infinite loop because the value would never change.
        if (step == 0) throw new ArgumentException("Step cannot be 0.", nameof(step));

        // Normalize negative step values. The method's direction is determined by start/end comparison.
        if (step < 0) step = Math.Abs(step);

        // Build into a mutable list, then return as IReadOnlyList<int> to discourage mutation by callers.
        var list = new List<int>();

        // Ascending range: keep adding step while staying within the inclusive end bound.
        if (start <= end)
        {
            for (int v = start; v <= end; v += step) list.Add(v);
        }
        // Descending range: subtract step while staying within the inclusive end bound.
        else
        {
            for (int v = start; v >= end; v -= step) list.Add(v);
        }

        return list;
    }
}
