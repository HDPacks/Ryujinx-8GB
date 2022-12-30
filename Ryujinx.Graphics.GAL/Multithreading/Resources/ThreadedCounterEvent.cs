﻿using Ryujinx.Graphics.GAL.Multithreading.Commands.CounterEvent;
using Ryujinx.Graphics.GAL.Multithreading.Model;
using System.Threading;

namespace Ryujinx.Graphics.GAL.Multithreading.Resources
{
    class ThreadedCounterEvent : ICounterEvent
    {
        private ThreadedRenderer _renderer;
        public ICounterEvent Base;

        public bool Invalid { get; set; }

        public CounterType Type { get; }
        public bool ClearCounter { get; }

        private bool _reserved;
        private int _createLock;

        public ThreadedCounterEvent(ThreadedRenderer renderer, CounterType type, bool clearCounter)
        {
            _renderer = renderer;
            Type = type;
            ClearCounter = clearCounter;
        }

        private TableRef<T> Ref<T>(T reference)
        {
            return new TableRef<T>(_renderer, reference);
        }

        public void Dispose()
        {
            _renderer.New<CounterEventDisposeCommand>().Set(Ref(this));
            _renderer.QueueCommand();
        }

        public void Flush()
        {
            ThreadedHelpers.SpinUntilNonNull(ref Base);

            Base.Flush();
        }

        public bool ReserveForHostAccess()
        {
            if (Base != null)
            {
                return Base.ReserveForHostAccess();
            }
            else
            {
                bool result = true;

                // A very light lock, as this case is uncommon.
                ThreadedHelpers.SpinUntilExchange(ref _createLock, 1, 0);

                if (Base != null)
                {
                    result = Base.ReserveForHostAccess();
                }
                else
                {
                    _reserved = true;
                }

                Volatile.Write(ref _createLock, 0);

                return result;
            }
        }

        public void Create(IRenderer renderer, CounterType type, System.EventHandler<ulong> eventHandler, bool hostReserved)
        {
            ThreadedHelpers.SpinUntilExchange(ref _createLock, 1, 0);
            Base = renderer.ReportCounter(type, eventHandler, hostReserved || _reserved);
            Volatile.Write(ref _createLock, 0);
        }
    }
}
