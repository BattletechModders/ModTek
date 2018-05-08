using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    public interface IModDef
    {
        string Directory { get; set; }
        string Name { get; set; }
        string Version { get; set; }
        DateTime? PackagedOn { get; set; }
        bool Enabled { get; set; }
        List<string> DependsOn { get; [UsedImplicitly] set; }
        List<string> ConflictsWith { get; set; }

        // ReSharper disable once InconsistentNaming
        string DLL { get; [UsedImplicitly] set; }

        // ReSharper disable once InconsistentNaming
        string DLLEntryPoint { get; [UsedImplicitly] set; }
        bool LoadImplicitManifest { get; set; }
        List<ModDef.ManifestEntry> Manifest { get; [UsedImplicitly] set; }
        JObject Settings { get; [UsedImplicitly] set; }
    }
}