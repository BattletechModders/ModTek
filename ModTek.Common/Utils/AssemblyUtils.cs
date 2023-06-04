using System.IO;
using System.Reflection;

namespace ModTek.Common.Utils;

internal static class AssemblyUtils
{
    internal static string GetAssemblyName(MemberInfo method)
    {
        return method.DeclaringType?.Assembly.GetName().Name;
    }

    internal static string GetFullName(MemberInfo method)
    {
        return method.DeclaringType?.FullName + "." + method.Name;
    }

    internal static string GetLocationOrName(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        try
        {
            // codebase points to the path of the loaded assembly
            // location points to the path of the original assembly location
            // if shimmed, this can differ, see preloader fake assembly location setting
            var codeBase = string.IsNullOrWhiteSpace(assembly.CodeBase) ? null : FileUtils.GetRelativePath(assembly.CodeBase);
            var location = string.IsNullOrWhiteSpace(assembly.Location) ? null : FileUtils.GetRelativePath(assembly.Location);

            var formatted = "";
            if (location == null || Path.GetFileNameWithoutExtension(location) != name)
            {
                formatted = name;
            }
            if (location != null)
            {
                if (formatted.Length > 0)
                {
                    formatted += " at ";
                }
                formatted += location;
            }
            if (codeBase != null && location != codeBase)
            {
                formatted += $" ({codeBase})";
            }
            return formatted;
        }
        catch
        {
            // ignored
        }
        return name;
    }
}