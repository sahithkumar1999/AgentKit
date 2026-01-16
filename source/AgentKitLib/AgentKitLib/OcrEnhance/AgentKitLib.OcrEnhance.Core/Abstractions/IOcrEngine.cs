using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

public interface IOcrEngine
{
    Task<OcrResult> ReadAsync(Stream image, OcrEngineOptions options, CancellationToken ct);
}
