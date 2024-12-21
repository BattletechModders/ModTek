using System.IO;

namespace ModTek.Features.Logging.LogStreamImpl;

// posix guarantees atomic appends to a file
// so instead of app level locks we get os level serialization
internal class MonoIoFileStreamImpl : ILogStream
{
    private readonly FileStream _stream;
    internal MonoIoFileStreamImpl(string path)
    {
        _stream = new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite|FileShare.Delete,
            1, // minimum size allowed, can't disable the buffer otherwise
            FileOptions.None
        );
        // disables use of non-thread safe internal buf_start within WriteInternal
        _ = _stream.SafeFileHandle;
    }

    public void Append(byte[] bytes, int srcOffset, int count)
    {
        _stream.Write(bytes, srcOffset, count);
    }

    public void FlushToDisk()
    {
        _stream.Flush(true);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}