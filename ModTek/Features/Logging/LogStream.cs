using System;
using ModTek.Features.Logging.LogStreamImpl;

namespace ModTek.Features.Logging;

internal class LogStream
{
    private readonly ILogStream _impl;
    internal LogStream(string path)
    {
        _impl = GetPlatformDependentLogStream(path);
    }

    private static ILogStream GetPlatformDependentLogStream(string path)
    {
        var os = Environment.OSVersion;

        if (os.Platform is PlatformID.Unix)
        {
            return new MonoIoFileStreamImpl(path);
        }

        if (os.Platform is PlatformID.Win32NT && os.Version.Major >= 10)
        {
            return new Win32ApiImpl(path);
        }

        return new FileStreamImpl(path);
    }

    public void Append(byte[] bytes, int srcOffset, int count)
    {
        _impl.Append(bytes, srcOffset, count);
    }

    public void FlushToDisk()
    {
        _impl.FlushToDisk();
    }

    public void Dispose()
    {
        _impl.Dispose();
    }
}