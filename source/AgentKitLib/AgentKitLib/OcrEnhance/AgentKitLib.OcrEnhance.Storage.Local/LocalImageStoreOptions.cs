using System;
using System.Collections.Generic;
using System.Text;

namespace AgentKitLib.OcrEnhance.Storage.Local;

public sealed class LocalImageStoreOptions
{
    public string RootDirectory { get; set; } = "image-store";
}