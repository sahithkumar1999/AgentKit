using AgentKit.OcrEnhance.Storage.Local;
using AgentKitLib.OcrEnhance.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Storage.Local;

public sealed class LocalImageStore : IImageStore
{
    private readonly LocalImageStoreOptions _opts;

    public LocalImageStore(LocalImageStoreOptions opts)
    {
        _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        Directory.CreateDirectory(_opts.RootDirectory);
    }

    public async Task<string> SaveAsync(Stream imageStream, string fileExtension, string? suggestedReference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileExtension)) fileExtension = ".png";
        if (!fileExtension.StartsWith('.')) fileExtension = "." + fileExtension;

        var reference = string.IsNullOrWhiteSpace(suggestedReference)
            ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
            : suggestedReference.Trim();

        var path = Path.Combine(_opts.RootDirectory, $"{reference}{fileExtension}");
        await using var fs = File.Create(path);
        await imageStream.CopyToAsync(fs, ct);
        return reference;
    }

    public Task<bool> ExistsAsync(string imageReference, CancellationToken ct)
    {
        var any = Directory.EnumerateFiles(_opts.RootDirectory, $"{imageReference}.*").Any();
        return Task.FromResult(any);
    }

    public Task<Stream> OpenReadAsync(string imageReference, CancellationToken ct)
    {
        var path = Directory.EnumerateFiles(_opts.RootDirectory, $"{imageReference}.*").FirstOrDefault();
        if (path is null) throw new FileNotFoundException($"Image reference not found: {imageReference}");

        Stream s = File.OpenRead(path);
        return Task.FromResult(s);
    }

    public async Task<string> SaveVariantAsync(Stream imageStream, string baseReference, string variantSuffix, string fileExtension, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileExtension)) fileExtension = ".png";
        if (!fileExtension.StartsWith('.')) fileExtension = "." + fileExtension;

        var variantRef = $"{baseReference}_{variantSuffix}";
        var path = Path.Combine(_opts.RootDirectory, $"{variantRef}{fileExtension}");

        await using var fs = File.Create(path);
        await imageStream.CopyToAsync(fs, ct);
        return variantRef;
    }
}
