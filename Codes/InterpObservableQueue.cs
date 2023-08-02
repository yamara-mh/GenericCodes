using Fusion;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Yamara
{
    /// <summary>
    /// A class that delay actions to match the Interpolation Target when the Interpolation DataSource is in Snapshots
    /// </summary>
    public class InterpObservableQueue<T>
    {
        private NetworkBehaviour nb;
        private readonly Queue<int> ticks = new();
        private readonly Queue<T> arguments = new();
        private Predicate<T> predicate;

        private Subject<T> subject = new();
        public IObservable<T> Observable => subject;

        /// <summary>
        /// Please run this function with Spawned()
        /// </summary>
        public InterpObservableQueue(NetworkBehaviour networkBehaviour, Predicate<T> predicate = null)
        {
            nb = networkBehaviour;
            this.predicate = predicate ??= _ => true;
        }

        public void Enqueue(T argument)
        {
            if (nb.Runner.IsResimulation)
            {
                Debug.LogWarning("If you Enqueue during IsResimulation, multiple events may be registered and cause problems. Enqueue did not.");
                return;
            }

            if (nb.Runner.IsServer || nb.InterpolationDataSource == NetworkBehaviour.InterpolationDataSources.Predicted)
            {
                subject.OnNext(argument);
                return;
            }

            int tick = nb.Runner.Tick + nb.Runner.Tick - nb.Runner.Simulation.InterpFrom.Tick;
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
                if (ticks.Peek() > nb.Runner.Tick) break;
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

        public void Clear(NetworkBehaviour networkBehaviour = null, Predicate<T> predicate = null)
        {
            nb = networkBehaviour;
            this.predicate = predicate ??= _ => true;
            ticks.Clear();
            arguments.Clear();
            subject.Dispose();
            subject = new();
        }
    }
}
