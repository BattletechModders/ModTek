using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Logging;

internal static unsafe class AsmRunner
{
    internal static MTStopwatch AsmRunnerStopwatch = new();
    internal static MTStopwatch ManualStopwatch = new();
    static AsmRunner()
    {
        const int Count = 100_000;
        var srcPtr = stackalloc char[256];
        srcPtr[0] = 'Y';
        srcPtr[1] = 'E';
        srcPtr[2] = 'S';
        var dstPtr = stackalloc byte[128];
        {
            var start = MTStopwatch.GetTimestamp();
            for (var i = 0; i < Count; i++)
            {
                s_function(srcPtr, dstPtr);
            }
            AsmRunnerStopwatch.EndMeasurement(start, Count);
        }
        {
            var charArray = new char[3];
            fixed (char* chars = charArray)
            {
                Encoding.UTF8.GetChars(dstPtr, 3, chars, 3);
            }
            FinalString = new string(charArray);
        }
        {
            var start = MTStopwatch.GetTimestamp();
            for (var i = 0; i < Count; i++)
            {
                s_function(srcPtr, dstPtr);
            }
            ManualStopwatch.EndMeasurement(start, Count);
        }
    }
    internal static readonly string FinalString;

    [SuppressUnmanagedCodeSecurity]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Function(char* srcPtr, byte* dstPtr);
    private static readonly Function s_function = setupFunction();
    private static Function setupFunction()
    {
        byte[] assembledCode =
        [
            0xc5, 0xf8, 0x10, 0x01,             // vmovups xmm0,XMMWORD PTR [rcx]
            0x62, 0xf2, 0x7e, 0x08, 0x30, 0xc0, // vpmovwb xmm0,xmm0
            0xc5, 0xf8, 0x10, 0x49, 0x0a,       // vmovups xmm1,XMMWORD PTR [rcx+0xa]
            0x62, 0xf2, 0x7e, 0x08, 0x30, 0xc9, // vpmovwb xmm1,xmm1
            0xc5, 0xf8, 0x16, 0xc1,             // vmovlhps xmm0,xmm0,xmm1
            0xc5, 0xf8, 0x11, 0x02,             // vmovups XMMWORD PTR [rdx],xmm0
            0xc3                                // ret
        ];

        fixed (byte* ptr = assembledCode)
        {
            var memoryAddress = (IntPtr) ptr;

            // Mark memory as EXECUTE_READWRITE to prevent DEP exceptions
            if (!VirtualProtectEx(Process.GetCurrentProcess().Handle, memoryAddress,
                    (UIntPtr) assembledCode.Length, 0x40 /* EXECUTE_READWRITE */, out _))
            {
                throw new Win32Exception();
            }

            return Marshal.GetDelegateForFunctionPointer<Function>(memoryAddress);
        }
    }
    [DllImport("kernel32.dll")]
    private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
}