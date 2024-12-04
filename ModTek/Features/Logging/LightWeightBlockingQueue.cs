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
    private volatile int _queueSize; // faster than calling _queue.Count
    private volatile bool _addingCompleted; // some way to shut down the thread

    // returns false if nothing can be dequeued anymore (empty + _addingCompleted)
    internal bool TryDequeueOrWait(out T item)
    {
        var spinWait = new SpinWait();
        while (!_queue.TryDequeue(out item))
        {
            if (_addingCompleted)
            {
                // this can still drop logs, very unlikely but possible
                Thread.Sleep(1);
                if (_queueSize == 0)
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
        // a compare exchange + retries in the loop would be more strict, but going over max is ok too (and faster)
        Interlocked.Increment(ref _queueSize);

        _queue.Enqueue(item);
        return true;
    }

    internal void CompleteAdding()
    {
        _addingCompleted = true;
    }
}