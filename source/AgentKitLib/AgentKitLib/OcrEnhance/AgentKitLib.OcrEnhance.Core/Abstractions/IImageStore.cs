using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

/// <summary>
/// Defines a storage abstraction for managing image assets and their enhancement variants.
/// This interface provides operations for saving, retrieving, and checking the existence of images
/// in a storage system (e.g., file system, blob storage, database).
/// </summary>
/// <remarks>
/// The <see cref="IImageStore"/> abstraction enables:
/// - Decoupling of image storage logic from the enhancement pipeline
/// - Support for multiple storage implementations (local file system, Azure Blob Storage, AWS S3, etc.)
/// - Consistent API for managing both original images and their enhanced variants
/// - Testability through mock implementations
/// 
/// Implementations should handle:
/// - Thread-safe concurrent access to stored images
/// - Proper resource cleanup (streams, file handles)
/// - Error handling for storage failures
/// - Unique reference generation for stored images
/// 
/// Typical workflow:
/// 1. SaveAsync() - Store an original image and get a unique reference
/// 2. Enhancement process reads the image using OpenReadAsync()
/// 3. SaveVariantAsync() - Store enhanced variants with relationship to the original
/// 4. ExistsAsync() - Check if images or variants exist before operations
/// </remarks>
public interface IImageStore
{
    /// <summary>
    /// Saves an image to the store and returns a unique reference identifier for retrieval.
    /// </summary>
    /// <param name="imageStream">
    /// The stream containing the image data to save. The stream should be positioned at the beginning.
    /// The implementation may read from this stream and is responsible for proper stream handling
    /// (but should NOT dispose of the stream unless documented otherwise).
    /// </param>
    /// <param name="fileExtension">
    /// The file extension indicating the image format (e.g., ".jpg", ".png", ".tiff").
    /// Should include the leading dot. Used to determine content type and appropriate storage format.
    /// </param>
    /// <param name="suggestedReference">
    /// An optional suggested reference identifier for the image. If provided and unique, implementations
    /// may use this as the reference. If null or already exists, the implementation should generate
    /// a new unique reference. This allows for predictable references when needed (e.g., resuming uploads).
    /// </param>
    /// <param name="ct">
    /// Cancellation token to observe for cancellation requests. Allows the save operation to be cancelled
    /// if it takes too long or is no longer needed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous save operation. The task result contains a unique reference
    /// identifier (string) that can be used to retrieve the image later via OpenReadAsync() or ExistsAsync().
    /// The reference format is implementation-specific but should be URL-safe and unique within the store.
    /// </returns>
    /// <remarks>
    /// Implementation considerations:
    /// - Generate unique references (e.g., GUID, hash of content, timestamp-based)
    /// - Validate that the stream contains valid image data (optional but recommended)
    /// - Store metadata along with the image if needed (size, content type, timestamp)
    /// - Handle duplicate references gracefully (either reject or generate new reference)
    /// - Ensure atomicity - either the image is fully saved or not at all
    /// 
    /// Example usage:
    /// <code>
    /// using var fileStream = File.OpenRead("document.jpg");
    /// string reference = await imageStore.SaveAsync(fileStream, ".jpg", null, cancellationToken);
    /// // reference can now be used to retrieve the image: "img-abc123-def456"
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when imageStream or fileExtension is null.</exception>
    /// <exception cref="IOException">Thrown when storage system encounters an I/O error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string> SaveAsync(
        Stream imageStream,
        string fileExtension,
        string? suggestedReference,
        CancellationToken ct);

    /// <summary>
    /// Opens a read-only stream for an image identified by its reference.
    /// </summary>
    /// <param name="imageReference">
    /// The unique reference identifier of the image to retrieve. This should be a reference
    /// previously returned by SaveAsync() or SaveVariantAsync().
    /// </param>
    /// <param name="ct">
    /// Cancellation token to observe for cancellation requests. Allows the open operation to be cancelled
    /// if it takes too long or is no longer needed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous open operation. The task result contains a readable Stream
    /// positioned at the beginning of the image data. The caller is responsible for disposing this stream
    /// when finished reading.
    /// </returns>
    /// <remarks>
    /// Implementation considerations:
    /// - Return a stream that supports reading (CanRead = true)
    /// - Position the stream at the beginning (Position = 0)
    /// - The stream may or may not support seeking (CanSeek) depending on the storage backend
    /// - Consider buffering for network-based storage to improve performance
    /// - The caller MUST dispose the returned stream to prevent resource leaks
    /// 
    /// Example usage:
    /// <code>
    /// using (Stream imageStream = await imageStore.OpenReadAsync(reference, cancellationToken))
    /// {
    ///     // Read and process the image
    ///     using var image = await Image.LoadAsync(imageStream);
    ///     // ... perform operations ...
    /// } // Stream is automatically disposed here
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when imageReference is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the image reference does not exist in the store.</exception>
    /// <exception cref="IOException">Thrown when storage system encounters an I/O error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<Stream> OpenReadAsync(string imageReference, CancellationToken ct);

    /// <summary>
    /// Checks whether an image with the specified reference exists in the store.
    /// </summary>
    /// <param name="imageReference">
    /// The unique reference identifier of the image to check. This should be a reference
    /// previously returned by SaveAsync() or SaveVariantAsync().
    /// </param>
    /// <param name="ct">
    /// Cancellation token to observe for cancellation requests. Allows the check operation to be cancelled
    /// if it takes too long or is no longer needed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous existence check operation. The task result is true if an image
    /// with the specified reference exists in the store; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method is useful for:
    /// - Validating references before attempting to read images
    /// - Checking if a variant has already been generated to avoid duplicate processing
    /// - Implementing caching strategies
    /// - Health checks and diagnostics
    /// 
    /// Implementation considerations:
    /// - This should be a lightweight operation (metadata check, not full content validation)
    /// - Should not throw exceptions for non-existent references (return false instead)
    /// - May cache results for performance if appropriate
    /// - Consider eventual consistency in distributed storage systems
    /// 
    /// Example usage:
    /// <code>
    /// if (await imageStore.ExistsAsync(reference, cancellationToken))
    /// {
    ///     var stream = await imageStore.OpenReadAsync(reference, cancellationToken);
    ///     // ... process image ...
    /// }
    /// else
    /// {
    ///     // Handle missing image scenario
    /// }
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when imageReference is null or empty.</exception>
    /// <exception cref="IOException">Thrown when storage system encounters an I/O error during the check.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<bool> ExistsAsync(string imageReference, CancellationToken ct);

    /// <summary>
    /// Saves an enhanced image variant and returns a unique reference identifier.
    /// This method is specifically designed for storing processed versions (variants) of a base image,
    /// maintaining a relationship between the original and its enhancements.
    /// </summary>
    /// <param name="imageStream">
    /// The stream containing the enhanced variant image data to save. The stream should be positioned
    /// at the beginning. The implementation may read from this stream and is responsible for proper
    /// stream handling (but should NOT dispose of the stream unless documented otherwise).
    /// </param>
    /// <param name="baseReference">
    /// The reference identifier of the original/base image that this variant is derived from.
    /// This creates a logical relationship between the original and the variant, which can be useful for:
    /// - Organizing related images together
    /// - Generating hierarchical storage paths
    /// - Cleaning up variants when the original is deleted
    /// - Tracking enhancement lineage
    /// </param>
    /// <param name="variantSuffix">
    /// A suffix that identifies this specific variant (e.g., "high-contrast", "denoised", "v1-sharpen").
    /// This suffix is typically incorporated into the variant's reference to make it distinguishable
    /// from other variants of the same base image. Should be URL-safe and not contain special characters.
    /// </param>
    /// <param name="fileExtension">
    /// The file extension indicating the image format (e.g., ".jpg", ".png", ".tiff").
    /// Should include the leading dot. The variant may have a different format than the original
    /// (e.g., converting to PNG for lossless storage of processed images).
    /// </param>
    /// <param name="ct">
    /// Cancellation token to observe for cancellation requests. Allows the save operation to be cancelled
    /// if it takes too long or is no longer needed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous save operation. The task result contains a unique reference
    /// identifier (string) for the variant that can be used to retrieve it later. The reference typically
    /// incorporates both the base reference and variant suffix for easy identification.
    /// Example: If baseReference is "img-abc123" and variantSuffix is "high-contrast", the returned
    /// reference might be "img-abc123-high-contrast" or "img-abc123/variants/high-contrast".
    /// </returns>
    /// <remarks>
    /// This method is specifically used for storing generated variants during the enhancement process.
    /// Each variant represents a different enhancement strategy applied to the original image.
    /// 
    /// Implementation considerations:
    /// - Construct variant references that clearly relate to the base reference
    /// - Store metadata linking variants to their base image
    /// - Consider organizing variants in subfolders/containers (e.g., "{baseRef}/variants/{suffix}")
    /// - Handle variant naming conflicts (same suffix for same base image)
    /// - Optionally store variant-specific metadata (enhancement plan, operation log, creation time)
    /// 
    /// Typical workflow:
    /// 1. Original image saved via SaveAsync() → returns "img-001"
    /// 2. Enhancement process creates multiple variants
    /// 3. Each variant saved via SaveVariantAsync() → returns "img-001-variant1", "img-001-variant2", etc.
    /// 4. Variants can be retrieved independently using their references
    /// 
    /// Example usage:
    /// <code>
    /// // Save original image
    /// string baseRef = await imageStore.SaveAsync(originalStream, ".jpg", null, ct);
    /// 
    /// // Apply enhancement and save variant
    /// using var enhancedStream = ApplyEnhancement(originalStream, plan);
    /// string variantRef = await imageStore.SaveVariantAsync(
    ///     enhancedStream, 
    ///     baseRef, 
    ///     "high-contrast", 
    ///     ".png", 
    ///     ct);
    /// // variantRef might be: "img-abc123-high-contrast"
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when any parameter (except ct) is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the baseReference doesn't exist in the store.</exception>
    /// <exception cref="IOException">Thrown when storage system encounters an I/O error.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    // Used for storing generated variants
    Task<string> SaveVariantAsync(
        Stream imageStream,
        string baseReference,
        string variantSuffix,
        string fileExtension,
        CancellationToken ct);
}
