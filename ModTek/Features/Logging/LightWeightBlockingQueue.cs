using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Features.Logging;

// this=40ns BlockingCollection=170ns
// does not really matter but we anyway get better suited APIs
internal class LightWeightBlockingQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
        
    private const int MaxQueueSize = 100_000; // probably about 30MB if full
    private long _queueSize; // faster than calling _queue.Count
    private bool _addingCompleted; // some way to shut down the thread

    // returns false if nothing can be dequeued anymore (empty + _addingCompleted)
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryDequeueOrWait(out T item)
    {
        var spinWait = new SpinWait();
        while (!_queue.TryDequeue(out item))
        {
            if (_addingCompleted)
            {
                // this can still drop logs, very unlikely but possible
                Thread.Sleep(1);
                if (_queue.IsEmpty)
                {
                    return false;
                }
            }
            spinWait.SpinOnce();
        }

        Interlocked.Decrement(ref _queueSize);
        return true;
    }

    // returns false if nothing can be enqueued anymore (_addingCompleted)
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryEnqueueOrWait(T item)
    {
        if (_addingCompleted)
        {
            return false;
        }

        while (_queueSize >= MaxQueueSize)
        {
            Thread.SpinWait(4);
                
            if (_addingCompleted)
            {
                return false;
            }
        }
        
        Interlocked.Increment(ref _queueSize);
        _queue.Enqueue(item);
        return true;
    }

    internal void CompleteAdding()
    {
        _addingCompleted = true;
    }
}