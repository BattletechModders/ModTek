using System.Reflection;
using MonoMod.Utils;

namespace HarmonyXInterop
{
    internal static class DMDUtil
    {
        public static MethodInfo GenerateWith<T>(this DynamicMethodDefinition dmd, object context = null)
            where T : DMDGenerator<T>, new()
        {
            return DMDGenerator<T>.Generate(dmd, context);
        }
    }
}