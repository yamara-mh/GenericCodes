using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Fusion
{
    /// <summary>
    /// A class that delay actions to match the Interpolation Target when the Interpolation DataSource is in Snapshots
    /// </summary>
    public class InterpObservableQueue<T>
    {
        private NetworkRunner runner;
        private readonly Queue<int> ticks = new();
        private readonly Queue<T> arguments = new();
        private Predicate<T> predicate;

        public bool IsPredicted;

        private Subject<T> subject = new();
        public IObservable<T> Observable => subject;

        /// <summary>
        /// Please run this function with Spawned()
        /// </summary>
        public InterpObservableQueue(NetworkRunner runner, bool isPredicted, Predicate<T> predicate = null)
        {
            this.runner = runner;
            this.predicate = predicate ??= _ => true;
            IsPredicted = isPredicted;
        }

        public void Enqueue(T argument)
        {
            if (runner.IsResimulation)
            {
                Debug.LogWarning("If you Enqueue during IsResimulation, multiple events may be registered and cause problems. Enqueue did not.");
                return;
            }

            if (runner.IsServer || IsPredicted)
            {
                subject.OnNext(argument);
                return;
            }

            int tick = runner.Tick + runner.Tick - runner.Simulation.InterpFrom.Tick;
            ticks.Enqueue(tick);
            arguments.Enqueue(argument);
        }

        /// <summary>
        /// Please run this function with FixedUpdateNetwork()
        /// </summary>
        public void UpdateQueues()
        {
            while (ticks.Count > 0)
            {
                if (ticks.Peek() > runner.Tick) break;
                ticks.Dequeue();
                var arg = arguments.Dequeue();
                if (predicate.Invoke(arg)) subject.OnNext(arg);
            }
        }

        public void ClearQueue()
        {
            ticks.Clear();
            arguments.Clear();
        }

        public void Kill()
        {
            runner = null;
            predicate = null;
            ticks.Clear();
            arguments.Clear();
            subject.Dispose();
            subject = null;
        }
    }
}
