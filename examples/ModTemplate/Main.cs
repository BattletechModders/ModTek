using System.Reflection;
using Harmony;
using HBS.Logging;

namespace ModTemplate;

public static class Main
{
    private static readonly ILog s_log = Logger.GetLogger(nameof(ModTemplate));
    public static void Start()
    {
        s_log.Log("Starting");

        HarmonyInstance
            .Create(nameof(ModTemplate))
            .PatchAll(Assembly.GetExecutingAssembly());

        s_log.Log("Started");
    }
}