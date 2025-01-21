using System.Diagnostics;
using System.Threading.RateLimiting;

namespace Aevatar.Ratelimt.Core;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Resources;

/// <summary>
/// <see cref="RateLimiter"/> implementation that refreshes allowed permits in a window periodically.
/// </summary>
public  class FixedWindowRateQueueLimiter : ReplenishingRateLimiter
{
    protected RateLimiter RateLimiter { get; set; }

    private readonly Queue<RequestRegistration> _queue = new Queue<RequestRegistration>();


    private object Lock => _queue;

    private readonly Timer? _renewTimer;
    
    private bool _disposed;
    
    private int _permitCount;


    private int _queueCount;

    private readonly FixedWindowRateLimiterOptions _options;
    


    private const string ShouldBeGreaterThan0Message = "The value of '{0}' must be greater than 0.";
    private const string ShouldBeGreaterThanOrEqual0Message = "The value of '{0}' must be greater than or equal to 0.";
    private const string ShouldBeGreaterThanTimeSpan0Message = "The value of '{0}' must be greater than TimeSpan.Zero.";
    
    private long _lastReplenishmentTick;
    private long? _idleSince;
    
    private long _successfulLeasesCount;

    
    private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

    private static readonly RateLimitLease SuccessfulLease = new FixedWindowLease(true, null);


    public override RateLimiterStatistics? GetStatistics()
    {
        return RateLimiter.GetStatistics();
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        throw new NotImplementedException();
    }

    public FixedWindowRateQueueLimiter(FixedWindowRateLimiterOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.PermitLimit <= 0)
        {
            throw new ArgumentException(
                string.Format(ShouldBeGreaterThan0Message, nameof(options.PermitLimit)), nameof(options.PermitLimit));
        }

        if (options.QueueLimit < 0)
        {
            throw new ArgumentException(
                string.Format(ShouldBeGreaterThanOrEqual0Message, nameof(options.QueueLimit)),
                nameof(options.QueueLimit));
        }

        if (options.Window <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(ShouldBeGreaterThanTimeSpan0Message, nameof(options.Window)), nameof(options.Window));
        }

        RateLimiter = new FixedWindowRateLimiter(options);

        _options = new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            QueueProcessingOrder = options.QueueProcessingOrder,
            QueueLimit = options.QueueLimit,
            Window = options.Window,
            AutoReplenishment = options.AutoReplenishment
        };

        // _permitCount = options.PermitLimit;
        //
        _idleSince = _lastReplenishmentTick = Stopwatch.GetTimestamp();

        if (_options.AutoReplenishment)
        {
            _renewTimer = new Timer(Replenish, this, _options.Window, _options.Window);
        }
    }

    private static void Replenish(object? state)
    {
        FixedWindowRateQueueLimiter limiter = (state as FixedWindowRateQueueLimiter)!;
        Debug.Assert(limiter is not null);

        // Use Stopwatch instead of DateTime.UtcNow to avoid issues on systems where the clock can change
        long nowTicks = Stopwatch.GetTimestamp();
        limiter!.ReplenishInternal(nowTicks);
    }

    // Used in tests that test behavior with specific time intervals
    private void ReplenishInternal(long nowTicks)
    {
        using var disposer = default(RequestRegistration.Disposer);

        // Method is re-entrant (from Timer), lock to avoid multiple simultaneous replenishes
        lock (Lock)
        {
            if (_disposed)
            {
                return;
            }

            if (((nowTicks - _lastReplenishmentTick) * TickFrequency) < _options.Window.Ticks &&
                !_options.AutoReplenishment)
            {
                return;
            }

            _lastReplenishmentTick = nowTicks;

            int availablePermitCounters = _permitCount;

            if (availablePermitCounters >= _options.PermitLimit)
            {
                // All counters available, nothing to do
                return;
            }

            _permitCount = _options.PermitLimit;

            // Process queued requests
            while (_queue.Count > 0)
            {
                RequestRegistration nextPendingRequest = _queue.Peek();
                   

                // Request was handled already, either via cancellation or being kicked from the queue due to a newer request being queued.
                // We just need to remove the item and let the next queued item be considered for completion.
                if (nextPendingRequest.Task.IsCompleted)
                {
                    nextPendingRequest =_queue.Peek();
                    disposer.Add(nextPendingRequest);
                }
                else if (_permitCount >= nextPendingRequest.Count)
                {
                    // Request can be fulfilled
                    nextPendingRequest =_queue.Peek();

                    _queueCount -= nextPendingRequest.Count;
                    _permitCount -= nextPendingRequest.Count;
                    Debug.Assert(_permitCount >= 0);

                    if (!nextPendingRequest.TrySetResult(SuccessfulLease))
                    {
                        // Queued item was canceled so add count back, permits weren't acquired
                        _permitCount += nextPendingRequest.Count;
                        if (!nextPendingRequest.QueueCountModified)
                        {
                            // We already updated the queue count, the Cancel code is about to run or running and waiting on our lock,
                            // tell Cancel not to do anything
                            nextPendingRequest.QueueCountModified = true;
                        }
                        else
                        {
                            // Updating queue count was handled by the cancellation code, don't double count
                            _queueCount += nextPendingRequest.Count;
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref _successfulLeasesCount);
                    }

                    disposer.Add(nextPendingRequest);
                    Debug.Assert(_queueCount >= 0);
                }
                else
                {
                    // Request cannot be fulfilled
                    break;
                }
            }

            if (_permitCount == _options.PermitLimit)
            {
                Debug.Assert(_idleSince is null);
                _idleSince = Stopwatch.GetTimestamp();
            }
        }
    }


    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount,
        CancellationToken cancellationToken)
    {
        var guid = Guid.NewGuid();

        RateLimitLease rateLimitLease = await RateLimiter.AcquireAsync(permitCount);

        if (rateLimitLease.IsAcquired)
        {
            // If permits are successfully acquired, return the lease immediately
            return rateLimitLease;
        }

        // If permits cannot be acquired, register the request in the queue
        var registration = new RequestRegistration(permitCount, this, cancellationToken);

        lock (Lock)
        {
            _queue.Enqueue(registration);
            _queueCount += permitCount;
            Debug.Assert(_queueCount <= _options.QueueLimit);
        }

        return await registration.Task;
    }


    public override TimeSpan? IdleDuration { get; }

    private sealed class FixedWindowLease : RateLimitLease
    {
        private static readonly string[] s_allMetadataNames = new[] { MetadataName.RetryAfter.Name };

        private readonly TimeSpan? _retryAfter;

        public FixedWindowLease(bool isAcquired, TimeSpan? retryAfter)
        {
            IsAcquired = isAcquired;
            _retryAfter = retryAfter;
        }

        public override bool IsAcquired { get; }

        public override IEnumerable<string> MetadataNames => s_allMetadataNames;

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name && _retryAfter.HasValue)
            {
                metadata = _retryAfter.Value;
                return true;
            }

            metadata = default;
            return false;
        }
    }

    private sealed class RequestRegistration : TaskCompletionSource<RateLimitLease>
    {
        private readonly CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        // Update under the limiter lock and only if the queue count was updated by the calling code
        public bool QueueCountModified { get; set; }

        // this field is used only by the disposal mechanics and never shared between threads
        private RequestRegistration? _next;

        public RequestRegistration(int permitCount, FixedWindowRateQueueLimiter limiter,
            CancellationToken cancellationToken)
            : base(limiter, TaskCreationOptions.RunContinuationsAsynchronously)
        {
            Count = permitCount;
            _cancellationToken = cancellationToken;

            // RequestRegistration objects are created while the limiter lock is held
            // if cancellationToken fires before or while the lock is held, UnsafeRegister
            // is going to invoke the callback synchronously, but this does not create
            // a deadlock because lock are reentrant
            if (cancellationToken.CanBeCanceled)
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                _cancellationTokenRegistration = cancellationToken.UnsafeRegister(Cancel, this);
#else
                    _cancellationTokenRegistration = cancellationToken.Register(Cancel, this);
#endif
        }

        public int Count { get; }

        private static void Cancel(object? state)
        {
            if (state is RequestRegistration registration &&
                registration.TrySetCanceled(registration._cancellationToken))
            {
                var limiter = (FixedWindowRateQueueLimiter)registration.Task.AsyncState!;
                lock (limiter.Lock)
                {
                    // Queuing and replenishing code might modify the _queueCount, since there is no guarantee of when the cancellation
                    // code runs and we only want to update the _queueCount once, we set a bool (under a lock) so either method
                    // can update the count and not double count.
                    if (!registration.QueueCountModified)
                    {
                        limiter._queueCount -= registration.Count;
                        registration.QueueCountModified = true;
                    }
                }
            }
        }

        /// <summary>
        /// Collects registrations to dispose outside the limiter lock to avoid deadlock.
        /// </summary>
        public struct Disposer : IDisposable
        {
            private RequestRegistration? _next;

            public void Add(RequestRegistration request)
            {
                request._next = _next;
                _next = request;
            }

            public void Dispose()
            {
                for (var current = _next; current is not null; current = current._next)
                {
                    current._cancellationTokenRegistration.Dispose();
                }

                _next = null;
            }
        }
    }

    public override bool TryReplenish()
    {
        throw new NotImplementedException();
    }

    public override TimeSpan ReplenishmentPeriod { get; }
    public override bool IsAutoReplenishing { get; }
}