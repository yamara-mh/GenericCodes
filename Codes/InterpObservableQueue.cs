using Fusion;
using System;
using System.Collections.Generic;
using UniRx;

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
        private Subject<T> subject = new();

        public IObservable<T> Observable => subject;

        /// <summary>
        /// Please run this function with Spawned()
        /// </summary>
        public InterpObservableQueue(NetworkBehaviour networkBehaviour)
        {
            nb = networkBehaviour;
        }

        public void Enqueue(T argument)
        {
            if (nb.Runner.IsResimulation) return;

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
            if (nb.Runner.IsResimulation) return;

            while (ticks.Count > 0)
            {
                if (ticks.Peek() > nb.Runner.Tick) break;
                ticks.Dequeue();
                subject.OnNext(arguments.Dequeue());
            }
        }

        public void Clear(NetworkBehaviour networkBehaviour = null)
        {
            nb = networkBehaviour;
            ticks.Clear();
            arguments.Clear();
            subject.Dispose();
            subject = new();
        }
    }
}
