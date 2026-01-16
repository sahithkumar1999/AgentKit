using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Imaging.OpenCv;
public sealed class OpenCvImageProcessor : IImageProcessor
{
    public Task<Stream> ApplyAsync(Stream inputImage, IReadOnlyList<PlanStep> steps, CancellationToken ct)
    {
        // Skeleton: return input unchanged for now.
        // Replace with: decode to Mat, apply ops, encode to PNG stream.
        // Ops to implement:
        // - zoom (crop/resize)
        // - rotate
        // - autocontrast/clahe
        // - denoise (median/bilateral)
        // - binarize (otsu/adaptive)
        // - deskew (Hough/angle estimation)
        // - sharpen (unsharp mask)
        var ms = new MemoryStream();
        inputImage.CopyTo(ms);
        ms.Position = 0;
        return Task.FromResult<Stream>(ms);
    }
}
