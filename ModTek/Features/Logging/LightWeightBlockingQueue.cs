#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Features.Logging;

// (this)RingBuffer with reference types=38ns
// ConcurrentQueue+custom size tracking+addingComplete=40ns
// BlockingCollection=170ns
internal class LightWeightBlockingQueue<T> where T : class
{
    private volatile bool _addingCompleted; // some way to shut down the thread

    // 100k leads to about ~30MB
    private const int MaxRingBufferSize = 1 << 16; // power of 2 required by FastModuloMaskForBitwiseAnd
    private const int MaxQueueSize = MaxRingBufferSize - 1; // Start=End need to be distinguishable
    // ring buffer is used by Disruptor(.NET), seems to work well for them
    // typed based exchanges are 56ns (fixed as of .NET 7) hence why we use object based ones
    private readonly object?[] _ringBuffer = new object?[MaxRingBufferSize];
    // end - start = size // all indexes are "excluding" basically, 0 means 0 not yet written (or read)
    // TODO how to avoid douple indexes?
    private volatile int _nextWritingIndex; // sync in between writers
    private volatile int _nextReadIndex; // sync between readers -> writers

    // see https://en.wikipedia.org/wiki/Modulo#Performance_issues
    // Bitwise AND is faster than modulo, just requires size to be power of 2
    // only gained like 1-2ns though (meaning it is within measurement error...)
    private const int FastModuloMaskForBitwiseAnd = MaxRingBufferSize - 1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Next(int index)
    {
        return (index + 1) & FastModuloMaskForBitwiseAnd;
        // return (index + 1) % MaxRingBufferSize;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Size(int startIndex, int endIndex)
    {
        return (endIndex - startIndex) & FastModuloMaskForBitwiseAnd;
        // return (endIndex - startIndex + MaxRingBufferSize) % MaxRingBufferSize;
    }

    // returns false if nothing can be dequeued anymore (empty + _addingCompleted)
    internal bool TryDequeueOrWait([NotNullWhen(true)] out T? item)
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var index = _nextReadIndex;
            if (Size(index, _nextWritingIndex) > 0)
            {
                item = Unsafe.As<T?>(Interlocked.Exchange(ref _ringBuffer[index], default));
                if (!ReferenceEquals(item, default))
                {
                    _nextReadIndex = Next(index);
                    return true;
                }
            }
            
            spinWait.SpinOnce(); // reader should yield and sleep if nothing comes in after some time
            
            if (_addingCompleted)
            {
                // this can still drop logs, very unlikely but possible
                Thread.Sleep(1);
                if (Size(_nextReadIndex, _nextWritingIndex) == 0)
                {
                    item = default;
                    return false;
                }
            }
        }
    }

    // returns false if nothing can be enqueued anymore (_addingCompleted)
    internal bool TryEnqueueOrWait(T item)
    {
        while (true)
        {
            if (_addingCompleted)
            {
                return false;
            }

            var index = _nextWritingIndex;
            if (Size(_nextReadIndex, index) < MaxQueueSize)
            {
                if (Interlocked.CompareExchange(ref _nextWritingIndex, Next(index), index) == index)
                {
                    _ringBuffer[index] = item;
                    return true;
                }
            }
            
            Thread.SpinWait(4); // main thread should always try to dispatch asap, never wait that much!
        }
    }

    internal void CompleteAdding()
    {
        _addingCompleted = true;
    }
}