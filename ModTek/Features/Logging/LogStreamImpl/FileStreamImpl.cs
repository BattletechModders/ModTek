using System.IO;

namespace ModTek.Features.Logging.LogStreamImpl;

internal class FileStreamImpl : ILogStream
{
    private readonly FileStream _stream;
    internal FileStreamImpl(string path)
    {
        _stream = new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite|FileShare.Delete,
            1, // small buffer size is equivalent to AutoFlush
            FileOptions.None
        );
    }

    public void Append(byte[] bytes, int srcOffset, int count)
    {
        lock (this)
        {
            _stream.Write(bytes, srcOffset, count);
        }
    }

    public void FlushToDisk()
    {
        lock (this)
        {
            _stream.Flush(true);
        }
    }

    public void Dispose()
    {
        lock (this)
        {
            _stream.Dispose();
        }
    }
}