using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKit.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines a strategy for generating an image reference (identifier) from the raw image bytes.
/// </summary>
/// <remarks>
/// This abstraction exists to keep reference generation:
/// - Consistent across the system (same input bytes -> same reference, if desired)
/// - Independent from the storage implementation (file system, blob storage, etc.)
/// - Configurable (hash-based IDs, random GUIDs, content-addressable references, etc.)
///
/// A common use case is content-addressable storage, where the reference is derived from a cryptographic
/// hash (e.g., SHA-256) of the image bytes. This provides a stable reference and can help with:
/// - Deduplication (same image content produces the same reference)
/// - Idempotent uploads (retrying the same image yields the same ID)
/// - Easy integrity verification (recompute hash and compare)
///
/// Implementations should consider:
/// - Output format (URL-safe string, case normalization, prefixes)
/// - Collision resistance (hash choice and encoding)
/// - Performance (avoid excessive allocations / copying)
/// - Whether to include additional entropy or metadata (usually avoid if "stable" refs are needed)
/// </remarks>
public interface IImageReferenceGenerator
{
    /// <summary>
    /// Creates an image reference from the provided image bytes.
    /// </summary>
    /// <param name="imageBytes">
    /// The raw bytes of the image content.
    /// 
    /// This is provided as a <see cref="ReadOnlySpan{T}"/> to enable efficient, allocation-free access
    /// to the underlying data. Implementations should avoid capturing the span or storing it anywhere,
    /// because its lifetime is only valid for the duration of the call.
    /// </param>
    /// <returns>
    /// A reference string that can be used as a stable identifier for the image.
    /// The returned value should be safe to use as:
    /// - A storage key (e.g., blob name / file name)
    /// - A path segment in a URL (recommended: base32/base64url/hex encoding)
    /// - A lookup key in a database
    /// </returns>
    /// <remarks>
    /// Recommended behavioral expectations (implementation-specific but strongly suggested):
    /// - Deterministic: The same image bytes should produce the same reference.
    /// - Non-empty: Never return null/empty/whitespace.
    /// - Normalized: Prefer a consistent casing and character set (e.g., lowercase hex).
    ///
    /// Example approach (hash-based):
    /// - Compute SHA-256 over <paramref name="imageBytes"/>
    /// - Encode as hex (64 chars) or base64url
    /// - Optionally prefix (e.g., "img_") to distinguish from other key types
    ///
    /// Example output:
    /// - "img_3f2a1c... (sha256 hex)"
    /// - "img_A1b2C3... (base64url)"
    /// </remarks>
    // “Innovative input” – generate stable refs, e.g., hash-based ID
    string CreateReference(ReadOnlySpan<byte> imageBytes);
}
