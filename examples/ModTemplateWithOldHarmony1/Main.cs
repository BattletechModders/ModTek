using System.Reflection;
using Harmony;
using HBS.Logging;

namespace ModTemplateWithOldHarmony1;

public static class Main
{
    private static readonly ILog s_log = Logger.GetLogger(nameof(ModTemplateWithOldHarmony1));
    public static void Start()
    {
        s_log.Log("Starting");

        HarmonyInstance
            .Create(nameof(ModTemplateWithOldHarmony1))
            .PatchAll(Assembly.GetExecutingAssembly());

        s_log.Log("Started");
    }
}