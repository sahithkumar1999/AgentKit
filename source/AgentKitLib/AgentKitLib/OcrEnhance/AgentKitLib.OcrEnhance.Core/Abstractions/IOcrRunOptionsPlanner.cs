using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

public interface IOcrRunOptionsPlanner
{
    Task<OcrRunOptions> CreateOptionsAsync(string prompt, CancellationToken ct);
}
