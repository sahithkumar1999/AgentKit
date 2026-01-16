using System;
using System.Collections.Generic;
using System.Text;
using AgentKitLib.OcrEnhance.Core.Models;

namespace AgentKitLib.OcrEnhance.Core.Abstractions;

public interface IPromptPlanner
{
    Task<EnhancementPlan> CreatePlanAsync(string userPrompt, CancellationToken ct);
}
