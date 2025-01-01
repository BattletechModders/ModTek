using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ModTek.Features.Logging.LogStreamImpl;

// avoids lock in the app
// still serializes the IO on OS level
// async is not used
internal class Win32ApiImpl : ILogStream
{
    private readonly SafeFileHandle _handle;
    private long _position;

    internal unsafe Win32ApiImpl(string path)
    {
        _handle = CreateFile(
            Path.GetFullPath(path),
            GENERIC_WRITE,
            FileShare.ReadWrite|FileShare.Delete,
            null,
            FileMode.Create,
            FILE_ATTRIBUTE_NORMAL,
            IntPtr.Zero
        );
        if (_handle.IsInvalid)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new IOException($"Win32Error {errorCode}");
        }
    }

    public unsafe void Append(byte[] bytes, int offset, int count)
    {
        if (bytes.Length - offset < count)
            throw new IndexOutOfRangeException();
        if (bytes.Length == 0)
        {
            return;
        }

        var position = AcquirePosition(count);
        var overlapped = PrepareOverlap(position);

        fixed (byte* numPtr = bytes)
        {
            if (WriteFile(_handle, numPtr + offset, count, out var numBytesWritten, &overlapped) != 0)
            {
                if (numBytesWritten != count)
                {
                    throw new IOException($"{numBytesWritten} != {count}");
                }
                return;
            }
        }

        var errorCode = Marshal.GetLastWin32Error();
        switch (errorCode)
        {
            case ERROR_INVALID_HANDLE:
                _handle.Dispose();
                return;
            case ERROR_INVALID_PARAMETER:
                throw new IOException($"Win32Error {errorCode} IO.IO_FileTooLongOrHandleNotSync");
            default:
                throw new IOException($"Win32Error {errorCode}");
        }
    }

    private long AcquirePosition(int count)
    {
        return Interlocked.Add(ref _position, count) - count;
    }

    private NativeOverlapped PrepareOverlap(long position)
    {
        NativeOverlapped overlapped = default;
        overlapped.OffsetLow = unchecked((int)position);
        overlapped.OffsetHigh = (int)(position >> 32);
        return overlapped;
    }

    public void FlushToDisk()
    {
        if (FlushFileBuffers(this._handle))
            return;

        var errorCode = Marshal.GetLastWin32Error();
        throw new IOException($"Win32Error {errorCode}");
    }

    public void Dispose()
    {
        if (!_handle.IsClosed)
        {
            _handle.Dispose();
        }
    }

    private const int GENERIC_WRITE = 0x40000000;
    private const int FILE_ATTRIBUTE_NORMAL = 0x80;
    private const int FILE_FLAG_RANDOM_ACCESS = 0x10000000;
    private const int FILE_FLAG_NO_BUFFERING = 0x20000000;
    private const int FILE_FLAG_OVERLAPPED = 0x40000000; // this is the async flag!
    // private const int FILE_FLAG_WRITE_THROUGH = (int)0x80000000;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
    private static extern unsafe SafeFileHandle CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        FileShare dwShareMode,
        SECURITY_ATTRIBUTES* lpSecurityAttributes,
        FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        internal uint nLength;
        internal unsafe void* lpSecurityDescriptor;
        internal BOOL bInheritHandle;
    }
    private enum BOOL : int
    {
        FALSE = 0,
        TRUE = 1,
    }

    private const int ERROR_INVALID_HANDLE = 6;
    private const int ERROR_INVALID_PARAMETER = 87;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe int WriteFile(
        SafeHandle handle,
        byte* bytes,
        int numBytesToWrite,
        out int numBytesWritten,
        NativeOverlapped* lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushFileBuffers(SafeHandle hHandle);
}