using System;
using System.IO;
using System.Linq;

namespace ModTek.Common.Utils;

internal class LogStream
{
    private readonly ILogStream _impl;
    internal LogStream(string path)
    {
        try
        {
            if (typeof(string).Assembly.GetTypes().Any(x => x.FullName == "System.IO.MonoIO"))
            {
                _impl = new MonoIoFileStreamImpl(path);
            }
            else
            {
                // win32 improvements
                throw new NotImplementedException();
            }
        }
        catch
        {
            _impl = new ThreadSafeFileStreamImpl(path);
        }
    }

    public void Append(byte[] bytes, int srcOffset, int count)
    {
        _impl.Append(bytes, srcOffset, count);
    }

    public void Dispose()
    {
        _impl.Dispose();
    }

    private interface ILogStream : IDisposable
    {
        public void Append(byte[] bytes, int srcOffset, int count);
    }

    private class MonoIoFileStreamImpl : ILogStream
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
            // skip check as our logging never logs less
            //EnsureMinimumSize(ref bytes, ref srcOffset, ref count);

            _stream.Write(bytes, srcOffset, count);
        }

        private const byte Linefeed = (byte)'\n';
        private const int ZeroWidthSize = 3; // ZWSP is unfortunately 3 bytes and not 2 in UTF8
        private const int ZeroWidthWithNewlineSize = 4;
        private static readonly byte[] s_zeroWidthSpaceWithNewline = [0xE2, 0x80, 0x8B, Linefeed];
        private static void EnsureMinimumSize(ref byte[] bytes, ref int srcOffset, ref int count)
        {
            // the buffer within FileStream is never thread safe
            // to avoid it we always have to write at least 2 bytes
            if (count >= 2)
            {
                return;
            }

            if (count > 0)
            {
                if (count == 1 && bytes[srcOffset] == Linefeed) // linux WriteLine()
                {
                    bytes = s_zeroWidthSpaceWithNewline;
                    srcOffset = 0;
                    count = ZeroWidthWithNewlineSize;
                }
                else
                {
                    var newBytes = new byte[ZeroWidthSize + count];
                    Buffer.BlockCopy(s_zeroWidthSpaceWithNewline, 0, newBytes, 0, ZeroWidthSize);
                    Buffer.BlockCopy(bytes, srcOffset, newBytes, ZeroWidthSize, count);
                    bytes = newBytes;
                    srcOffset = 0;
                    count = ZeroWidthSize + count;
                }
            }
            else
            {
                bytes = s_zeroWidthSpaceWithNewline;
                srcOffset = 0;
                count = ZeroWidthSize;
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
    
    private class ThreadSafeFileStreamImpl : ILogStream
    {
        private readonly FileStream _stream;
        internal ThreadSafeFileStreamImpl(string path)
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

        public void Dispose()
        {
            lock (this)
            {
                _stream.Dispose();
            }
        }
    }
}
