using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using AgentKitLib.OcrEnhance.Core.Models;
using OpenCvSharp;

namespace AgentKitLib.OcrEnhance.Imaging.OpenCv;

/// <summary>
/// OpenCV-based implementation of <see cref="IImageProcessor"/> that applies an ordered set of
/// preprocessing steps to an input image to improve downstream OCR quality.
/// </summary>
/// <remarks>
/// <para>
/// This processor is designed to execute the steps produced by the enhancement planning layer
/// (<see cref="IPromptPlanner"/> → <see cref="EnhancementPlan"/>), applying each <see cref="PlanStep"/> in order.
/// </para>
/// <para>
/// The output is encoded as PNG to preserve detail (lossless) and provide a stable format for OCR engines.
/// </para>
/// <para>
/// The current implementation loads the entire image into memory for decoding. For very large images,
/// consider implementing a temporary file strategy.
/// </para>
/// </remarks>
public sealed class OpenCvImageProcessor : IImageProcessor
{
    /// <summary>
    /// Applies all requested plan steps to the input image stream and returns a new image stream
    /// containing the processed result.
    /// </summary>
    /// <param name="inputImage">
    /// Source image stream. The caller owns the stream; this method will not dispose it.
    /// The stream may be non-seekable.
    /// </param>
    /// <param name="steps">
    /// Ordered preprocessing steps to apply. Unknown operations fail fast to avoid silent no-ops.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Stream"/> positioned at the beginning containing the processed image encoded as PNG.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the input image cannot be decoded by OpenCV.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the plan contains an unsupported operation.
    /// </exception>
    public Task<Stream> ApplyAsync(Stream inputImage, IReadOnlyList<PlanStep> steps, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Read the entire input stream into memory to support reliable OpenCV decoding.
        using var inputMs = new MemoryStream();
        inputImage.CopyTo(inputMs);
        var inputBytes = inputMs.ToArray();

        if (inputBytes.Length == 0)
            throw new InvalidOperationException("OpenCV could not decode the input image because the input stream was empty.");

        // Decode image bytes into an OpenCV Mat (BGR color).
        using var src = Cv2.ImDecode(inputBytes, ImreadModes.Color);
        if (src.Empty())
        {
            // Log a small header sample to aid diagnosis (wrong file, empty/corrupt bytes, unsupported format, etc.)
            var header = BitConverter.ToString(inputBytes.Take(Math.Min(16, inputBytes.Length)).ToArray());
            throw new InvalidOperationException(
                $"OpenCV could not decode the input image (empty Mat). Bytes={inputBytes.Length}, Header[0..15]={header}");
        }

        // Clone into a mutable Mat that is transformed in-place.
        using var current = src.Clone();

        foreach (var step in steps ?? Array.Empty<PlanStep>())
        {
            ct.ThrowIfCancellationRequested();

            // Normalize op name and pull optional parameter bag (case-insensitive).
            var op = (step.Op ?? string.Empty).Trim().ToLowerInvariant();
            var p = step.Params ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            switch (op)
            {
                case "":
                    // Empty operation => no-op.
                    break;

                case "rotate":
                    ApplyRotateInPlace(current, GetDouble(p, "angle", fallback: 0));
                    break;

                case "zoom":
                    ApplyZoomInPlace(current, p);
                    break;

                case "autocontrast":
                    ApplyAutoContrastInPlace(current, GetDouble(p, "cutoff", fallback: 0.01));
                    break;

                case "clahe":
                    ApplyClaheInPlace(
                        current,
                        clipLimit: GetDouble(p, "clipLimit", fallback: 2.0),
                        tileGridSize: GetInt(p, "tileGridSize", fallback: 8));
                    break;

                case "denoise":
                    ApplyDenoiseInPlace(current, GetString(p, "strength", fallback: "light"));
                    break;

                case "binarize":
                    ApplyBinarizeInPlace(current, p);
                    break;

                case "brightness":
                    ApplyBrightnessInPlace(current, GetDouble(p, "delta", fallback: 0));
                    break;

                case "gamma":
                    ApplyGammaInPlace(current, GetDouble(p, "value", fallback: 1.0));
                    break;

                case "sharpen":
                    ApplySharpenInPlace(
                        current,
                        amount: GetDouble(p, "amount", fallback: GetDouble(p, "strength", fallback: 1.2)),
                        sigma: GetDouble(p, "sigma", fallback: 1.0));
                    break;

                case "deskew":
                    // TODO: implement skew estimation + rotation (currently a safe no-op).
                    break;

                default:
                    // Unknown op => fail fast so plans don’t silently do nothing.
                    throw new NotSupportedException($"Unsupported op: '{step.Op}'.");
            }
        }

        // Encode as PNG (lossless; typically good defaults for OCR preprocessing).
        var pngBytes = current.ImEncode(".png");

        Stream output = new MemoryStream(pngBytes);
        output.Position = 0;
        return Task.FromResult(output);
    }

    /// <summary>
    /// Rotates the image around its center by the specified angle, expanding the canvas to avoid cropping.
    /// </summary>
    private static void ApplyRotateInPlace(Mat imgBgr, double angleDegrees)
    {
        if (Math.Abs(angleDegrees) < 0.0001)
            return;

        var center = new Point2f(imgBgr.Width / 2f, imgBgr.Height / 2f);
        using var rot = Cv2.GetRotationMatrix2D(center, angleDegrees, 1.0);

        // Compute a new bounding box to avoid cropping after rotation.
        var cos = Math.Abs(rot.Get<double>(0, 0));
        var sin = Math.Abs(rot.Get<double>(0, 1));
        var newW = (int)Math.Round(imgBgr.Height * sin + imgBgr.Width * cos);
        var newH = (int)Math.Round(imgBgr.Height * cos + imgBgr.Width * sin);

        // Translate to keep rotated image centered in the expanded canvas.
        rot.Set(0, 2, rot.Get<double>(0, 2) + (newW / 2.0) - center.X);
        rot.Set(1, 2, rot.Get<double>(1, 2) + (newH / 2.0) - center.Y);

        using var dst = new Mat();
        Cv2.WarpAffine(imgBgr, dst, rot, new Size(newW, newH), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.White);
        dst.CopyTo(imgBgr);
    }

    /// <summary>
    /// Resizes the image based on either a scale factor or explicit width/height.
    /// </summary>
    private static void ApplyZoomInPlace(Mat imgBgr, Dictionary<string, object> p)
    {
        // Support either scale OR (width/height).
        var hasWidth = p.Keys.Any(k => k.Equals("width", StringComparison.OrdinalIgnoreCase));
        var hasHeight = p.Keys.Any(k => k.Equals("height", StringComparison.OrdinalIgnoreCase));

        Size target;
        if (hasWidth || hasHeight)
        {
            var w = GetInt(p, "width", imgBgr.Width);
            var h = GetInt(p, "height", imgBgr.Height);
            target = new Size(Math.Max(1, w), Math.Max(1, h));
        }
        else
        {
            var scale = GetDouble(p, "scale", fallback: 1.0);
            if (scale <= 0) scale = 1.0;
            target = new Size(
                Math.Max(1, (int)Math.Round(imgBgr.Width * scale)),
                Math.Max(1, (int)Math.Round(imgBgr.Height * scale)));
        }

        using var dst = new Mat();
        Cv2.Resize(imgBgr, dst, target, 0, 0, InterpolationFlags.Cubic);
        dst.CopyTo(imgBgr);
    }

    /// <summary>
    /// Performs a basic auto-contrast stretch on the LAB luminance channel using the given cutoff percentage.
    /// </summary>
    private static void ApplyAutoContrastInPlace(Mat imgBgr, double cutoff)
    {
        // cutoff = percentage (0..1) of pixels to clip at low/high ends
        cutoff = Math.Clamp(cutoff, 0, 0.49);

        // Work in LAB; stretch L channel.
        using var lab = new Mat();
        Cv2.CvtColor(imgBgr, lab, ColorConversionCodes.BGR2Lab);

        Cv2.Split(lab, out var channels);
        using var l = channels[0];
        using var a = channels[1];
        using var b = channels[2];

        // Compute histogram on L.
        int histSize = 256;
        Rangef histRange = new Rangef(0, 256);
        using var hist = new Mat();
        Cv2.CalcHist(
            images: new[] { l },
            channels: new[] { 0 },
            mask: null,
            hist: hist,
            dims: 1,
            histSize: new[] { histSize },
            ranges: new[] { histRange });

        double total = l.Rows * l.Cols;
        double clip = total * cutoff;

        int low = 0, high = 255;
        double acc = 0;
        for (int i = 0; i < 256; i++)
        {
            acc += hist.Get<float>(i);
            if (acc >= clip) { low = i; break; }
        }

        acc = 0;
        for (int i = 255; i >= 0; i--)
        {
            acc += hist.Get<float>(i);
            if (acc >= clip) { high = i; break; }
        }

        if (high <= low)
            return;

        // Stretch L to [0,255] using OpenCV ops.
        using var lFloat = new Mat();
        l.ConvertTo(lFloat, MatType.CV_32F);

        Cv2.Subtract(lFloat, new Scalar(low), lFloat);
        Cv2.Multiply(lFloat, new Scalar(255.0 / (high - low)), lFloat);
        Cv2.Min(lFloat, 255, lFloat);
        Cv2.Max(lFloat, 0, lFloat);

        using var lOut = new Mat();
        lFloat.ConvertTo(lOut, MatType.CV_8U);

        using var merged = new Mat();
        Cv2.Merge(new[] { lOut, a, b }, merged);
        Cv2.CvtColor(merged, imgBgr, ColorConversionCodes.Lab2BGR);
    }

    /// <summary>
    /// Applies Contrast Limited Adaptive Histogram Equalization (CLAHE) to the LAB luminance channel.
    /// </summary>
    private static void ApplyClaheInPlace(Mat imgBgr, double clipLimit, int tileGridSize)
    {
        clipLimit = Math.Max(0.1, clipLimit);
        tileGridSize = Math.Max(2, tileGridSize);

        using var lab = new Mat();
        Cv2.CvtColor(imgBgr, lab, ColorConversionCodes.BGR2Lab);

        Cv2.Split(lab, out var channels);
        using var l = channels[0];
        using var a = channels[1];
        using var b = channels[2];

        using var clahe = Cv2.CreateCLAHE(clipLimit, new Size(tileGridSize, tileGridSize));
        using var lOut = new Mat();
        clahe.Apply(l, lOut);

        using var merged = new Mat();
        Cv2.Merge(new[] { lOut, a, b }, merged);
        Cv2.CvtColor(merged, imgBgr, ColorConversionCodes.Lab2BGR);
    }

    /// <summary>
    /// Applies a simple denoise filter with coarse strength presets.
    /// </summary>
    private static void ApplyDenoiseInPlace(Mat imgBgr, string strength)
    {
        strength = (strength ?? "light").Trim().ToLowerInvariant();

        using var dst = new Mat();

        // V1: simple, fast filters.
        switch (strength)
        {
            case "strong":
                // Bilateral filter preserves edges reasonably well, but is slower.
                Cv2.BilateralFilter(imgBgr, dst, d: 9, sigmaColor: 75, sigmaSpace: 75);
                break;

            case "medium":
                Cv2.MedianBlur(imgBgr, dst, ksize: 5);
                break;

            default: // light
                Cv2.MedianBlur(imgBgr, dst, ksize: 3);
                break;
        }

        dst.CopyTo(imgBgr);
    }

    /// <summary>
    /// Converts the image to a binary (black/white) representation.
    /// Supports <c>method=otsu</c> (default), <c>method=adaptive</c>, or a fixed threshold.
    /// </summary>
    private static void ApplyBinarizeInPlace(Mat imgBgr, Dictionary<string, object> p)
    {
        var method = GetString(p, "method", fallback: "otsu").Trim().ToLowerInvariant();

        using var gray = new Mat();
        Cv2.CvtColor(imgBgr, gray, ColorConversionCodes.BGR2GRAY);

        using var bin = new Mat();

        if (method == "adaptive")
        {
            var blockSize = GetInt(p, "blockSize", fallback: 21);
            if (blockSize < 3) blockSize = 3;
            if (blockSize % 2 == 0) blockSize += 1;

            var c = GetDouble(p, "c", fallback: 5);

            Cv2.AdaptiveThreshold(
                gray, bin,
                maxValue: 255,
                adaptiveMethod: AdaptiveThresholdTypes.GaussianC,
                thresholdType: ThresholdTypes.Binary,
                blockSize: blockSize,
                c);
        }
        else
        {
            // Otsu (default) or fixed threshold.
            if (p.TryGetValue("threshold", out _))
            {
                var t = GetDouble(p, "threshold", fallback: 128);
                Cv2.Threshold(gray, bin, t, 255, ThresholdTypes.Binary);
            }
            else
            {
                Cv2.Threshold(gray, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            }
        }

        // Convert back to 3-channel so later ops (if any) can assume BGR.
        Cv2.CvtColor(bin, imgBgr, ColorConversionCodes.GRAY2BGR);
    }

    /// <summary>
    /// Applies a simple brightness shift by adding <paramref name="delta"/> to all pixels.
    /// </summary>
    private static void ApplyBrightnessInPlace(Mat imgBgr, double delta)
    {
        if (Math.Abs(delta) < 0.0001)
            return;

        using var dst = new Mat();
        imgBgr.ConvertTo(dst, imgBgr.Type(), alpha: 1.0, beta: delta);
        dst.CopyTo(imgBgr);
    }

    /// <summary>
    /// Applies gamma correction using a precomputed lookup table (LUT) for efficiency.
    /// </summary>
    private static void ApplyGammaInPlace(Mat imgBgr, double gamma)
    {
        gamma = Math.Clamp(gamma, 0.1, 10.0);
        if (Math.Abs(gamma - 1.0) < 0.0001)
            return;

        var inv = 1.0 / gamma;

        // LUT for speed.
        using var lut = new Mat(1, 256, MatType.CV_8U);
        for (int i = 0; i < 256; i++)
        {
            var v = Math.Pow(i / 255.0, inv) * 255.0;
            lut.Set(0, i, (byte)Math.Clamp((int)Math.Round(v), 0, 255));
        }

        using var dst = new Mat();
        Cv2.LUT(imgBgr, lut, dst);
        dst.CopyTo(imgBgr);
    }

    /// <summary>
    /// Sharpens the image via an unsharp mask (Gaussian blur + weighted subtraction).
    /// </summary>
    private static void ApplySharpenInPlace(Mat imgBgr, double amount, double sigma)
    {
        amount = Math.Clamp(amount, 0.0, 5.0);
        sigma = Math.Clamp(sigma, 0.1, 10.0);

        if (amount <= 0)
            return;

        using var blurred = new Mat();
        Cv2.GaussianBlur(imgBgr, blurred, new Size(0, 0), sigma);

        // Unsharp mask: dst = img*(1+amount) + blurred*(-amount)
        using var dst = new Mat();
        Cv2.AddWeighted(imgBgr, 1.0 + amount, blurred, -amount, 0, dst);
        dst.CopyTo(imgBgr);
    }

    /// <summary>
    /// Reads a string parameter from a generic plan parameter bag.
    /// </summary>
    private static string GetString(Dictionary<string, object> p, string key, string fallback)
    {
        if (p.TryGetValue(key, out var v) && v is not null)
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? fallback;
        return fallback;
    }

    /// <summary>
    /// Reads an integer parameter from a generic plan parameter bag supporting common numeric representations.
    /// </summary>
    private static int GetInt(Dictionary<string, object> p, string key, int fallback)
    {
        if (!p.TryGetValue(key, out var v) || v is null)
            return fallback;

        if (v is int i) return i;
        if (v is long l) return checked((int)l);
        if (v is double d) return (int)Math.Round(d);

        if (int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return fallback;
    }

    /// <summary>
    /// Reads a double parameter from a generic plan parameter bag supporting common numeric representations.
    /// </summary>
    private static double GetDouble(Dictionary<string, object> p, string key, double fallback)
    {
        if (!p.TryGetValue(key, out var v) || v is null)
            return fallback;

        if (v is double d) return d;
        if (v is float f) return f;
        if (v is int i) return i;
        if (v is long l) return l;

        if (double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return fallback;
    }
}
