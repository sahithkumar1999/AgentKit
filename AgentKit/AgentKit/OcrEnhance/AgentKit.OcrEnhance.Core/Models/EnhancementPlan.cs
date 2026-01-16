using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;


namespace AgentKit.OcrEnhance.Core.Models;

/// <summary>
/// Represents a comprehensive plan for image enhancement operations designed to optimize images for OCR processing.
/// This model supports multiple variant strategies, allowing A/B testing of different preprocessing approaches
/// to determine which produces the best OCR results for specific use cases.
/// </summary>
/// <remarks>
/// An EnhancementPlan can contain multiple variants, where each variant represents a different enhancement strategy.
/// This allows for parallel processing and comparison of results to find the optimal preprocessing configuration.
/// The plan is serializable to/from JSON for easy configuration and storage.
/// </remarks>
public sealed class EnhancementPlan
{
    /// <summary>
    /// Gets or sets the collection of enhancement variants.
    /// Each variant represents a different preprocessing strategy with its own sequence of operations.
    /// </summary>
    /// <remarks>
    /// Multiple variants enable:
    /// - Testing different enhancement approaches on the same image
    /// - Parallel processing of multiple strategies
    /// - Selection of the best result based on OCR confidence scores
    /// - Fallback options if one strategy fails
    /// Default: Empty list (no variants defined)
    /// </remarks>
    [JsonPropertyName("variants")]
    public List<PlanVariant> Variants { get; set; } = [];
}

/// <summary>
/// Represents a single enhancement strategy variant within an EnhancementPlan.
/// A variant contains an ordered sequence of enhancement operations (steps) that are applied to an image.
/// </summary>
/// <remarks>
/// Variants allow you to define different preprocessing approaches such as:
/// - "high-contrast": Focused on contrast enhancement for low-quality scans
/// - "denoised": Emphasis on noise reduction for images with artifacts
/// - "skew-corrected": Specialized for handling rotated or tilted documents
/// - "standard": General-purpose enhancement for typical use cases
/// Steps within a variant are executed sequentially in the order they are defined.
/// </remarks>
public sealed class PlanVariant
{
    /// <summary>
    /// Gets or sets the descriptive name for this variant.
    /// Used to identify and distinguish between different enhancement strategies.
    /// </summary>
    /// <remarks>
    /// Best practices for naming:
    /// - Use descriptive names that indicate the strategy (e.g., "high-contrast-binarized")
    /// - Keep names unique within an EnhancementPlan
    /// - Use lowercase with hyphens for consistency (e.g., "ocr-optimized")
    /// Default: "variant"
    /// </remarks>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "variant";

    /// <summary>
    /// Gets or sets the ordered sequence of enhancement operations (steps) to apply to the image.
    /// Steps are executed in the order they appear in this list, forming a processing pipeline.
    /// </summary>
    /// <remarks>
    /// The order of steps is critical as each operation builds upon the results of previous operations.
    /// Common step sequences:
    /// 1. Geometric corrections (deskew, rotate) - Fix orientation first
    /// 2. Noise reduction (denoise) - Clean the image
    /// 3. Contrast enhancement (autocontrast, clahe) - Improve visibility
    /// 4. Sharpening (sharpen) - Enhance edges and text definition
    /// 5. Binarization (binarize) - Final conversion for OCR (if needed)
    /// Default: Empty list (no operations)
    /// </remarks>
    [JsonPropertyName("steps")]
    public List<PlanStep> Steps { get; set; } = [];
}

/// <summary>
/// Defines a single enhancement operation (step) within a variant's processing pipeline.
/// Each step specifies an operation type and its associated parameters.
/// </summary>
/// <remarks>
/// Steps are the atomic units of image enhancement. Each step performs a specific transformation
/// on the image, and multiple steps can be chained together to create complex enhancement workflows.
/// The flexible parameter system allows each operation type to accept custom configuration
/// while maintaining a consistent interface.
/// </remarks>
public sealed class PlanStep
{
    /// <summary>
    /// Gets or sets the operation identifier/name that specifies which enhancement operation to perform.
    /// </summary>
    /// <remarks>
    /// Supported operations:
    /// - "zoom": Scale or resize the image to optimal resolution for OCR
    /// - "rotate": Rotate the image by a specified angle to correct orientation
    /// - "autocontrast": Automatically adjust image contrast for better text visibility
    /// - "clahe": Apply Contrast Limited Adaptive Histogram Equalization for advanced contrast enhancement
    /// - "denoise": Remove noise and artifacts that interfere with OCR accuracy
    /// - "binarize": Convert to binary (black and white) format, often improving OCR for clear documents
    /// - "deskew": Correct skewed/tilted text alignment for images captured at angles
    /// - "sharpen": Enhance edge definition and text clarity for better character recognition
    /// 
    /// Operation names are case-sensitive and should be lowercase.
    /// The actual implementation of each operation is handled by the processing engine.
    /// Default: Empty string (no operation specified)
    /// </remarks>
    // Example: "zoom", "rotate", "autocontrast", "clahe", "denoise", "binarize", "deskew", "sharpen"
    [JsonPropertyName("op")]
    public string Op { get; set; } = "";

    /// <summary>
    /// Gets or sets the generic parameter dictionary for operation-specific configuration.
    /// This flexible parameter bag allows each operation type to accept custom parameters without
    /// requiring changes to the model structure.
    /// </summary>
    /// <remarks>
    /// The dictionary uses case-insensitive string keys for parameter names, making it more forgiving
    /// when parsing user-provided configuration (e.g., "strength" and "Strength" are treated as the same key).
    /// 
    /// Parameter values are stored as objects, allowing for different data types:
    /// - Numbers (int, double): For thresholds, strengths, angles, etc.
    /// - Strings: For method names, modes, or text values
    /// - Booleans: For enabling/disabling features
    /// - Complex objects: For advanced configuration (when needed)
    /// 
    /// Example parameters by operation:
    /// - zoom: { "scale": 2.0, "interpolation": "bicubic" }
    /// - rotate: { "angle": 90, "expand": true }
    /// - sharpen: { "strength": 1.5, "radius": 1.0 }
    /// - binarize: { "method": "adaptive", "blockSize": 11, "C": 2 }
    /// - clahe: { "clipLimit": 2.0, "tileGridSize": 8 }
    /// - denoise: { "strength": "medium", "searchWindowSize": 21 }
    /// 
    /// If an operation doesn't require parameters, this can be an empty dictionary.
    /// Default: Empty dictionary (no parameters specified)
    /// </remarks>
    // Generic params bag (keep flexible)
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}