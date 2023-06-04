using System;
using System.IO;

namespace ModTek.Common.Globals;

internal class CommonPaths
{
    private const string ENV_DOORSTOP_MANAGED_FOLDER_DIR = "DOORSTOP_MANAGED_FOLDER_DIR";
    internal static readonly string ManagedDirectory = Environment.GetEnvironmentVariable(ENV_DOORSTOP_MANAGED_FOLDER_DIR)
        ?? throw new Exception($"Can't find {ENV_DOORSTOP_MANAGED_FOLDER_DIR}");
    internal static readonly string BaseDirectory = Path.GetFullPath(Path.Combine(ManagedDirectory, "..", ".."));

    internal static readonly string ModsDirectory = Path.Combine(BaseDirectory, "Mods");
    internal static readonly string ModTekDirectory = Path.Combine(ModsDirectory, "ModTek");
    internal static readonly string DotModTekDirectory = Path.Combine(ModsDirectory, ".modtek");
}