using Microsoft.Build.Framework;

namespace ModTek.InjectorsTask;

// from Krafs.Publicizer
internal static class TaskItemExtensions
{
    internal static string FileName(this ITaskItem item)
    {
        return item.GetMetadata("Filename");
    }

    internal static string FullPath(this ITaskItem item)
    {
        return item.GetMetadata("Fullpath");
    }
}