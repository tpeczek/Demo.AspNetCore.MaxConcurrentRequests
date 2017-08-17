using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Demo.AspNetCore.MaxConcurrentRequests.Middlewares.Internals
{
    internal class MaxConcurrentRequestsEnqueuer
    {
        public enum DropMode
        {
            Tail = MaxConcurrentRequestsLimitExceededPolicy.FifoQueueDropTail,
            Head = MaxConcurrentRequestsLimitExceededPolicy.FifoQueueDropHead
        }

        #region Fields
        private readonly object _lock = new object();

        private readonly int _maxQueueLength;
        private readonly DropMode _dropMode;
        private readonly int _maxTimeInQueue;

        private readonly LinkedList<TaskCompletionSource<bool>> _queue = new LinkedList<TaskCompletionSource<bool>>();

        private static readonly Task<bool> _enqueueFailedTask = Task.FromResult(false);
        #endregion

        #region Constructor
        public MaxConcurrentRequestsEnqueuer(int maxQueueLength, DropMode dropMode, int maxTimeInQueue)
        {
            _maxQueueLength = maxQueueLength;
            _dropMode = dropMode;
            _maxTimeInQueue = maxTimeInQueue;
        }
        #endregion

        #region Methods
        public Task<bool> EnqueueAsync(CancellationToken cancellationToken)
        {
            Task<bool> enqueueTask = _enqueueFailedTask;

            if (_maxQueueLength > 0)
            {
                lock (_lock)
                {
                    if (_queue.Count < _maxQueueLength)
                    {
                        enqueueTask = InternalEnqueueAsync(cancellationToken);
                    }
                    else if (_dropMode == DropMode.Head)
                    {
                        InternalDequeue(false);

                        enqueueTask = InternalEnqueueAsync(cancellationToken);
                    }
                }
            }

            return enqueueTask;
        }

        public bool Dequeue()
        {
            bool dequeued = false;

            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    InternalDequeue(true);
                    dequeued = true;
                }
            }

            return dequeued;
        }

        private Task<bool> InternalEnqueueAsync(CancellationToken cancellationToken)
        {
            Task<bool> enqueueTask = _enqueueFailedTask;

            TaskCompletionSource <bool> enqueueTaskCompletionSource = new TaskCompletionSource<bool>();

            CancellationToken enqueueCancellationToken = GetEnqueueCancellationToken(enqueueTaskCompletionSource, cancellationToken);

            if (!enqueueCancellationToken.IsCancellationRequested)
            {
                _queue.AddLast(enqueueTaskCompletionSource);
                enqueueTask = enqueueTaskCompletionSource.Task;
            }

            return enqueueTask;
        }

        private CancellationToken GetEnqueueCancellationToken(TaskCompletionSource<bool> enqueueTaskCompletionSource, CancellationToken cancellationToken)
        {
            CancellationToken enqueueCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                GetTimeoutToken(enqueueTaskCompletionSource)
            ).Token;

            enqueueCancellationToken.Register(CancelEnqueue, enqueueTaskCompletionSource);

            return enqueueCancellationToken;
        }

        private CancellationToken GetTimeoutToken(TaskCompletionSource<bool> enqueueTaskCompletionSource)
        {
            CancellationToken timeoutToken = CancellationToken.None;

            if (_maxTimeInQueue != MaxConcurrentRequestsOptions.MaxTimeInQueueUnlimited)
            {
                CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();

                timeoutToken = timeoutTokenSource.Token;
                timeoutToken.Register(CancelEnqueue, enqueueTaskCompletionSource);

                timeoutTokenSource.CancelAfter(_maxTimeInQueue);
            }

            return timeoutToken;
        }

        private void CancelEnqueue(object state)
        {
            bool removed = false;

            TaskCompletionSource<bool> enqueueTaskCompletionSource = ((TaskCompletionSource<bool>)state);
            lock (_lock)
            {
                removed = _queue.Remove(enqueueTaskCompletionSource);
            }

            if (removed)
            {
                enqueueTaskCompletionSource.SetResult(false);
            }
        }

        private void InternalDequeue(bool result)
        {
            TaskCompletionSource<bool> enqueueTaskCompletionSource = _queue.First.Value;

            _queue.RemoveFirst();

            enqueueTaskCompletionSource.SetResult(result);
        }
        #endregion
    }
}
