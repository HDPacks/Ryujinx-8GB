using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    class KTimeManager : IDisposable
    {
        public static readonly long DefaultTimeIncrementNanoseconds = ConvertGuestTicksToNanoseconds(2);

        private class WaitingObject
        {
            public IKFutureSchedulerObject Object { get; }
            public long TimePoint { get; }

            public WaitingObject(IKFutureSchedulerObject schedulerObj, long timePoint)
            {
                Object = schedulerObj;
                TimePoint = timePoint;
            }
        }

        private readonly KernelContext _context;
        private readonly List<WaitingObject> _waitingObjects;
        private AutoResetEvent _waitEvent;
        private bool _keepRunning;
        private long _enforceWakeupFromSpinWait;

        private const long NanosecondsPerSecond = 1000000000L;
        private const long NanosecondsPerMillisecond = 1000000L;

        public KTimeManager(KernelContext context)
        {
            _context = context;
            _waitingObjects = new List<WaitingObject>();
            _keepRunning = true;

            Thread work = new Thread(WaitAndCheckScheduledObjects)
            {
                Name = "HLE.TimeManager"
            };

            work.Start();
        }

        public void ScheduleFutureInvocation(IKFutureSchedulerObject schedulerObj, long timeout)
        {
            long startTime = PerformanceCounter.ElapsedTicks;
            long timePoint = startTime + ConvertNanosecondsToHostTicks(timeout);

            if (timePoint < startTime)
            {
                timePoint = long.MaxValue;
            }

            lock (_context.CriticalSection.Lock)
            {
                _waitingObjects.Add(new WaitingObject(schedulerObj, timePoint));

                if (timeout < NanosecondsPerMillisecond)
                {
                    Interlocked.Exchange(ref _enforceWakeupFromSpinWait, 1);
                }
            }

            _waitEvent.Set();
        }

        public void UnscheduleFutureInvocation(IKFutureSchedulerObject schedulerObj)
        {
            lock (_context.CriticalSection.Lock)
            {
                _waitingObjects.RemoveAll(x => x.Object == schedulerObj);
            }
        }

        private void WaitAndCheckScheduledObjects()
        {
            SpinWait spinWait = new SpinWait();
            WaitingObject next;

            using (_waitEvent = new AutoResetEvent(false))
            {
                while (_keepRunning)
                {
                    lock (_context.CriticalSection.Lock)
                    {
                        Interlocked.Exchange(ref _enforceWakeupFromSpinWait, 0);

                        next = GetNextWaitingObject();
                    }

                    if (next != null)
                    {
                        long timePoint = PerformanceCounter.ElapsedTicks;

                        if (next.TimePoint > timePoint)
                        {
                            long ms = Math.Min((next.TimePoint - timePoint) / PerformanceCounter.TicksPerMillisecond, int.MaxValue);

                            if (ms > 0)
                            {
                                _waitEvent.WaitOne((int)ms);
                            }
                            else
                            {
                                while (Interlocked.Read(ref _enforceWakeupFromSpinWait) != 1 && PerformanceCounter.ElapsedTicks <= next.TimePoint)
                                {
                                    if (spinWait.NextSpinWillYield)
                                    {
                                        Thread.Yield();

                                        spinWait.Reset();
                                    }

                                    spinWait.SpinOnce();
                                }

                                spinWait.Reset();
                            }
                        }

                        bool timeUp = PerformanceCounter.ElapsedTicks >= next.TimePoint;

                        if (timeUp)
                        {
                            lock (_context.CriticalSection.Lock)
                            {
                                if (_waitingObjects.Remove(next))
                                {
                                    next.Object.TimeUp();
                                }
                            }
                        }
                    }
                    else
                    {
                        _waitEvent.WaitOne();
                    }
                }
            }
        }

        private WaitingObject GetNextWaitingObject()
        {
            WaitingObject selected = null;

            long lowestTimePoint = long.MaxValue;

            for (int index = _waitingObjects.Count - 1; index >= 0; index--)
            {
                WaitingObject current = _waitingObjects[index];

                if (current.TimePoint <= lowestTimePoint)
                {
                    selected = current;
                    lowestTimePoint = current.TimePoint;
                }
            }

            return selected;
        }

        public static long ConvertNanosecondsToMilliseconds(long time)
        {
            time /= NanosecondsPerMillisecond;

            if ((ulong)time > int.MaxValue)
            {
                return int.MaxValue;
            }

            return time;
        }

        public static long ConvertMillisecondsToNanoseconds(long time)
        {
            return time * NanosecondsPerMillisecond;
        }

        public static long ConvertNanosecondsToHostTicks(long ns)
        {
            long nsDiv = ns / NanosecondsPerSecond;
            long nsMod = ns % NanosecondsPerSecond;
            long tickDiv = PerformanceCounter.TicksPerSecond / NanosecondsPerSecond;
            long tickMod = PerformanceCounter.TicksPerSecond % NanosecondsPerSecond;

            long baseTicks = (nsMod * tickMod + PerformanceCounter.TicksPerSecond - 1) / NanosecondsPerSecond;
            return (nsDiv * tickDiv) * NanosecondsPerSecond + nsDiv * tickMod + nsMod * tickDiv + baseTicks;
        }

        public static long ConvertGuestTicksToNanoseconds(long ticks)
        {
            return (long)Math.Ceiling(ticks * (1000000000.0 / 19200000.0));
        }

        public static long ConvertHostTicksToTicks(long time)
        {
            return (long)((time / (double)PerformanceCounter.TicksPerSecond) * 19200000.0);
        }

        public void Dispose()
        {
            _keepRunning = false;
            _waitEvent?.Set();
        }
    }
}