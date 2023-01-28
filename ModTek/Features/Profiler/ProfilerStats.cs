using System;
using System.Text;

namespace ModTek.Features.Profiler;

internal class ProfilerStats : IEquatable<ProfilerStats>
{
    private static ProfilerStats s_lastStats;
    internal static void LogIfChanged()
    {
        var current = new ProfilerStats();
        if (current.Equals(s_lastStats))
        {
            return;
        }

        s_lastStats = current;
        Log.Profiler.Info?.Log(current.ToString());
    }

    internal long GetUsedHeapSize = ToMB(UnityEngine.Profiling.Profiler.usedHeapSizeLong);
    internal long GetMonoHeapSizeLong = ToMB(UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong());
    internal long GetMonoUsedSizeLong = ToMB(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
    internal long GetTempAllocatorSize = ToMB(UnityEngine.Profiling.Profiler.GetTempAllocatorSize());
    internal long GetTotalAllocatedMemoryLong = ToMB(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
    internal long GetTotalUnusedReservedMemoryLong = ToMB(UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong());
    internal long GetTotalReservedMemoryLong = ToMB(UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong());
    internal long GetAllocatedMemoryForGraphicsDriver = ToMB(UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver());

    public bool Equals(ProfilerStats other)
    {
        if (other == null)
        {
            return false;
        }

        return GetMonoUsedSizeLong == other.GetMonoUsedSizeLong;
    }

    private static long ToMB(long byteCount)
    {
        return byteCount / 1024 / 1024;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("Stats\n");
        sb.Append(nameof(GetUsedHeapSize));
        sb.Append(": ");
        sb.Append(GetUsedHeapSize);
        sb.Append("\n");
        sb.Append(nameof(GetMonoHeapSizeLong));
        sb.Append(": ");
        sb.Append(GetMonoHeapSizeLong);
        sb.Append("\n");
        sb.Append(nameof(GetMonoUsedSizeLong));
        sb.Append(": ");
        sb.Append(GetMonoUsedSizeLong);
        sb.Append("\n");
        sb.Append(nameof(GetTempAllocatorSize));
        sb.Append(": ");
        sb.Append(GetTempAllocatorSize);
        sb.Append("\n");
        sb.Append(nameof(GetTotalAllocatedMemoryLong));
        sb.Append(": ");
        sb.Append(GetTotalAllocatedMemoryLong);
        sb.Append("\n");
        sb.Append(nameof(GetTotalUnusedReservedMemoryLong));
        sb.Append(": ");
        sb.Append(GetTotalUnusedReservedMemoryLong);
        sb.Append("\n");
        sb.Append(nameof(GetTotalReservedMemoryLong));
        sb.Append(": ");
        sb.Append(GetTotalReservedMemoryLong);
        sb.Append("\n");
        sb.Append(nameof(GetAllocatedMemoryForGraphicsDriver));
        sb.Append(": ");
        sb.Append(GetAllocatedMemoryForGraphicsDriver);
        return sb.ToString();
    }
}
