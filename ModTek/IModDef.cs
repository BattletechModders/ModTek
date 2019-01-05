using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    public interface IModDef
    {
        string Name { get; set; }
        string Description { get; set; }
        string Author { get; set; }
        string Website { get; set; }
        string Contact { get; set; }

        bool Enabled { get; set; }

        string Version { get; set; }
        DateTime? PackagedOn { get; set; }

        HashSet<string> DependsOn { get; set; }
        HashSet<string> ConflictsWith { get; set; }
        HashSet<string> OptionallyDependsOn { get; set; }

        string DLL { get; set; }
        string DLLEntryPoint { get; set; }

        bool LoadImplicitManifest { get; set; }
        List<ModDef.ManifestEntry> Manifest { get; set; }

        JObject Settings { get; set; }
    }
}
