using System;

namespace ModTek.Features.Logging.LogStreamImpl;

internal interface ILogStream : IDisposable
{
    public void Append(byte[] bytes, int srcOffset, int count);

    public void FlushToDisk();
}