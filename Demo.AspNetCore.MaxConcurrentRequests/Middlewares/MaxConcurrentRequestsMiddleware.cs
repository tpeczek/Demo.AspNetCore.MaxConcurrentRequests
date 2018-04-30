using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Demo.AspNetCore.MaxConcurrentRequests.Middlewares.Internals;

namespace Demo.AspNetCore.MaxConcurrentRequests.Middlewares
{
    public class MaxConcurrentRequestsMiddleware
    {
        #region Fields
        private int _concurrentRequestsCount;

        private readonly RequestDelegate _next;
        private readonly MaxConcurrentRequestsOptions _options;
        private readonly MaxConcurrentRequestsEnqueuer _enqueuer;
        #endregion

        #region Constructor
        public MaxConcurrentRequestsMiddleware(RequestDelegate next, IOptions<MaxConcurrentRequestsOptions> options)
        {
            _concurrentRequestsCount = 0;

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (_options.LimitExceededPolicy != MaxConcurrentRequestsLimitExceededPolicy.Drop)
            {
                _enqueuer = new MaxConcurrentRequestsEnqueuer(_options.MaxQueueLength, (MaxConcurrentRequestsEnqueuer.DropMode)_options.LimitExceededPolicy, _options.MaxTimeInQueue);
            }
        }
        #endregion

        #region Methods
        public async Task Invoke(HttpContext context)
        {
            if (CheckLimitExceeded() && !(await TryWaitInQueueAsync(context.RequestAborted)))
            {
                if (!context.RequestAborted.IsCancellationRequested)
                {
                    IHttpResponseFeature responseFeature = context.Features.Get<IHttpResponseFeature>();

                    responseFeature.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    responseFeature.ReasonPhrase = "Concurrent request limit exceeded.";
                }
            }
            else
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    if (await ShouldDecrementConcurrentRequestsCountAsync())
                    {
                        Interlocked.Decrement(ref _concurrentRequestsCount);
                    }
                }
            }
        }

        private bool CheckLimitExceeded()
        {
            bool limitExceeded;

            if (_options.Limit == MaxConcurrentRequestsOptions.ConcurrentRequestsUnlimited)
            {
                limitExceeded = false;
            }
            else
            {
                int initialConcurrentRequestsCount, incrementedConcurrentRequestsCount;
                do
                {
                    limitExceeded = true;

                    initialConcurrentRequestsCount = _concurrentRequestsCount;
                    if (initialConcurrentRequestsCount >= _options.Limit)
                    {
                        break;
                    }

                    limitExceeded = false;
                    incrementedConcurrentRequestsCount = initialConcurrentRequestsCount + 1;
                }
                while (initialConcurrentRequestsCount != Interlocked.CompareExchange(ref _concurrentRequestsCount, incrementedConcurrentRequestsCount, initialConcurrentRequestsCount));
            }

            return limitExceeded;
        }

        private async Task<bool> TryWaitInQueueAsync(CancellationToken requestAbortedCancellationToken)
        {
            return (_enqueuer != null) && (await _enqueuer.EnqueueAsync(requestAbortedCancellationToken));
        }

        private async Task<bool> ShouldDecrementConcurrentRequestsCountAsync()
        {
            return (_options.Limit != MaxConcurrentRequestsOptions.ConcurrentRequestsUnlimited)
                && ((_enqueuer == null) || !(await _enqueuer.DequeueAsync()));
        }
        #endregion
    }
}
