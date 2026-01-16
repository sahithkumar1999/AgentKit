using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Core.Models
{
    public sealed record ImageAsset(
        string Reference,
        string FileName,
        string ContentType,
        long SizeBytes,
        int? Width,
        int? Height,
        DateTimeOffset CreatedUtc
    );
}

