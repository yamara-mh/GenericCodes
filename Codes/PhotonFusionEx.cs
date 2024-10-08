// #define FUSION1

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Fusion;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using static Fusion.NetworkBehaviour;

namespace Generic
{
    public static class PhotonFusionEx
    {
        #region Tick

        public static Tick ToTick(this int tick) => tick;
        public static Tick ToTick(this int second, NetworkRunner runner) => Mathf.CeilToInt(second / runner.DeltaTime);
        public static Tick ToTick(this float second, NetworkRunner runner) => Mathf.CeilToInt(second / runner.DeltaTime);
        public static Tick ToTick(this double second, NetworkRunner runner) => Mathf.CeilToInt((float)second / runner.DeltaTime);
        public static float ToSecond(this Tick tick, NetworkRunner runner) => tick * runner.DeltaTime;

        public static Tick GetTickAfter(this NetworkRunner runner, float second) => runner.Tick + (int)(second / runner.DeltaTime);

        public static int ElapsedTick(this NetworkRunner runner, Tick tick) => runner.Tick - tick;
        public static float ElapsedTime(this NetworkRunner runner, Tick tick) => (runner.Tick - tick) * runner.DeltaTime;
        public static int RemainingTick(this NetworkRunner runner, Tick tick) => tick - runner.Tick;
        public static float RemainingTime(this NetworkRunner runner, Tick tick) => -runner.ElapsedTime(tick);

        public static bool IsAt(this NetworkRunner runner, Tick tick) => runner.Tick == tick;
        public static bool HasReached(this NetworkRunner runner, Tick tick) => (runner.Tick - tick) >= 0;
        public static bool HasNotReached(this NetworkRunner runner, Tick tick) => (runner.Tick - tick) < 0;

#if FUSION1
        public static double SimulationRenderTime(this NetworkRunner runner, Tick offsetTick = 0)
            => runner.Simulation.StatePrevious.Tick - offsetTick + runner.Simulation.StateAlpha * runner.Simulation.DeltaTime;
        public static double InterpolationRenderTime(this NetworkRunner runner, Tick offsetTick = 0)
            => (runner.IsServer ? runner.Tick : runner.Simulation.InterpFrom.Tick) - offsetTick + runner.Simulation.InterpAlpha * runner.Simulation.DeltaTime;
        public static float InterpolationSecond(this NetworkRunner runner)
            => runner.IsServer ? 0f : (runner.Simulation.InterpTo.Tick - runner.Simulation.InterpFrom.Tick) * runner.DeltaTime;
#endif

        #endregion

        #region TickTimer
        public static Observable<Unit> OnCompleted(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate()
                .Select(_ => tickTimer.Expired(runner))
                .DistinctUntilChanged()
                .Where(completed => completed)
                .Select(_ => Unit.Default)
                .Publish().RefCount();

        public static Observable<Unit> OnUpdated(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate().Where(_ => !tickTimer.Expired(runner)).Select(_ => Unit.Default)
                .Publish().RefCount();

        public static Observable<float> OnRunnerUpdated(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate()
                .Where(_ => !tickTimer.Expired(runner))
                .Select(_ => tickTimer.RemainingTime(runner).Value)
                .DistinctUntilChanged()
                .Publish().RefCount();

        public static Observable<int> OnCountDowned(this TickTimer tickTimer, NetworkRunner runner, int minCount = 1, int maxCount = int.MaxValue)
            => Observable.EveryUpdate()
                .Where(_ => !tickTimer.Expired(runner))
                .Select(_ => Mathf.CeilToInt(tickTimer.RemainingTime(runner).Value))
                .DistinctUntilChanged()
                .Where(count => count >= minCount && count <= maxCount)
                .Publish().RefCount();
        #endregion

        #region Network Array, LinkedList, Dictionary

        public static void ForEach<T>(this NetworkArray<T> array, Action<T> action)
        {
            foreach (var item in array) action.Invoke(item);
        }
        public static void ForEach<T>(this NetworkArray<T> array, NetworkArray<T> oldArray, Action<T, T> action)
            => ForLoop(array, oldArray, (i, prev, current) => action.Invoke(prev, current));
        public static void ForLoop<T>(this NetworkArray<T> array, Action<int, T> action)
        {
            for (int i = 0; i < array.Length; i++) action.Invoke(i, array[i]);
        }
        public static void ForLoop<T>(this NetworkArray<T> array, NetworkArray<T> oldArray, Action<int, T, T> action)
        {
            for (int i = 0; i < array.Length; i++) action.Invoke(i, oldArray[i], array[i]);
        }
        public static void Replace<T>(this NetworkArray<T> array, Func<T, bool> conditions, Func<T, T> value)
        {
            for (int i = 0; i < array.Length; i++) if (conditions.Invoke(array.Get(i))) array.Set(i, value.Invoke(array.Get(i)));
        }
        public static void Replace<T>(this NetworkArray<T> array, Func<T, bool> conditions, T value) => Replace(array, conditions, v => value);
        public static int ReplaceOne<T>(this NetworkArray<T> array, Func<T, bool> conditions, Func<T, T> value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (conditions.Invoke(array.Get(i)))
                {
                    array.Set(i, value.Invoke(array.Get(i)));
                    return i;
                }
            }
            return -1;
        }
        public static int ReplaceOne<T>(this NetworkArray<T> array, Func<T, bool> conditions, T value) => ReplaceOne(array, conditions, v => value);
        public static bool ReplaceOneByOne<T>(this NetworkArray<T> array, Func<T, bool> conditions, params T[] values)
        {
            var index = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (conditions.Invoke(array.Get(i)))
                {
                    array.Set(i, values[index++]);
                    if (values.Length == index) return true;
                }
            }
            return false;
        }
        public static void ReplaceAll<T>(this NetworkArray<T> array, Func<T, T> value)
        {
            for (int i = 0; i < array.Length; i++) array.Set(i, value.Invoke(array.Get(i)));
        }
        public static void ReplaceAll<T>(this NetworkArray<T> array, T value) => ReplaceAll(array, v => value);


        public static T Get<T>(this NetworkArray<T> array, int capacity1, int index1, int index2)
            => array.Get(capacity1 * index1 + index2);
        public static T Set<T>(NetworkArray<T> array, T value, int capacity1, int index1, int index2)
            => array.Set(capacity1 * index1 + index2, value);

#if !FUSION1
        public static void OnValueChanged<T>(this NetworkArray<T> currentArray, NetworkArrayReadOnly<T> prevArray, Action<T> action) where T : IEquatable<T>
        {
            for (int i = 0; i < prevArray.Length; i++)
            {
                if (!currentArray[i].Equals(prevArray[i])) action?.Invoke(currentArray[i]);
            }
        }
        public static void OnValueChanged<T>(this NetworkArray<T> currentArray, NetworkArrayReadOnly<T> prevArray, Action<int, T, T> action) where T : IEquatable<T>
        {
            for (int i = 0; i < prevArray.Length; i++)
            {
                if (!currentArray[i].Equals(prevArray[i])) action?.Invoke(i, prevArray[i], currentArray[i]);
            }
        }
#else
        public static void OnValueChanged<Class, T>(this NetworkArray<T> currentArray, Changed<Class> changed, Func<Class, NetworkArray<T>> loadArray, Action<T> action)
            where Class : NetworkBehaviour
            where T : IEquatable<T>
        {
            var prevArray = new NetworkArray<T>();
            changed.LoadOld(old => prevArray = loadArray.Invoke(old));

            for (int i = 0; i < prevArray.Length; i++)
            {
                if (!currentArray[i].Equals(prevArray[i])) action?.Invoke(currentArray[i]);
            }
        }
        public static void OnValueChanged<Class, T>(this NetworkArray<T> currentArray, Changed<Class> changed, Func<Class, NetworkArray<T>> loadArray, Action<int, T, T> action)
            where Class : NetworkBehaviour
            where T : IEquatable<T>
        {
            var prevArray = new NetworkArray<T>();
            changed.LoadOld(old => prevArray = loadArray.Invoke(old));

            for (int i = 0; i < prevArray.Length; i++)
            {
                if (!currentArray[i].Equals(prevArray[i])) action?.Invoke(i, prevArray[i], currentArray[i]);
            }
        }
#endif

        public static void CopyFrom<T>(this NetworkArray<T> netArray, T[] array) => netArray.CopyFrom(array, 0, array.Length - 1);

        public static void CopyFrom<T>(this NetworkLinkedList<T> netList, List<T> list)
        {
            if (list.Count == netList.Count)
            {
                for (int i = 0; i < list.Count; i++) netList.Set(i, list[i]);
            }
            else
            {
                netList.Clear();
                for (int i = 0; i < list.Count; i++) netList.Add(list[i]);
            }
        }

        public static void CopyFrom<K, V>(this NetworkDictionary<K, V> netDict, KeyValuePair<K, V>[] pairs)
        {
            netDict.Clear();
            for (int i = 0; i < pairs.Length; i++) netDict.Add(pairs[i].Key, pairs[i].Value);
        }

        #endregion

        #region NetworkBehaviour, NetworkObject

        public static bool IsAtSnapshot(this NetworkObject no) => no.LastReceiveTick == no.Runner.Tick;
        public static bool IsAtSnapshot(this NetworkBehaviour nb) => nb.Object.LastReceiveTick == nb.Runner.Tick;
        public static bool IsAtInput(this NetworkObject no) => no.HasInputAuthority ? no.Runner.IsForward : no.IsAtSnapshot();
        public static bool IsAtInput(this NetworkBehaviour nb) => nb.HasInputAuthority ? nb.Runner.IsForward : nb.IsAtSnapshot();

        // MEMO : Please change the method of obtaining the random number seed as appropriate for your game.
        public static int GetSeed(this NetworkRunner runner) => runner.SessionInfo.Name.GetHashCode();
        public static int GetSeed(this NetworkBehaviour nb) => unchecked(nb.Runner.GetSeed() + nb.Id.Behaviour);
        public static int GetSeed(this NetworkBehaviour nb, Tick tick) => unchecked((nb.Runner.GetSeed() + (nb.Id.Behaviour + 1) * tick) | 1);

        public static T FindBehaviour<T>(this NetworkRunner runner) where T : SimulationBehaviour
           => runner.GetAllBehaviours<T>().FirstOrDefault();
        public static bool FindBehaviour<T>(this NetworkRunner runner, out T behaviour) where T : SimulationBehaviour
        {
            behaviour = runner.GetAllBehaviours<T>().FirstOrDefault();
            return behaviour != null;
        }
        public static async UniTask<T> FindBehaviourAsync<T>(this NetworkRunner runner, CancellationToken token) where T : SimulationBehaviour
        {
            while (token.IsCancellationRequested == false)
            {
                if (runner.FindBehaviour<T>(out var behaviour)) return behaviour;
                await UniTask.DelayFrame(1);
            }
            return default;
        }

        public static T FindBehaviour<T>(this NetworkRunner runner, PlayerRef player) where T : SimulationBehaviour
            => runner.GetAllBehaviours<T>().FirstOrDefault(b => b.Object.InputAuthority == player);
        public static bool FindBehaviour<T>(this NetworkRunner runner, PlayerRef player, out T behaviour) where T : SimulationBehaviour
        {
            behaviour = runner.FindBehaviour<T>(player);
            return behaviour != null;
        }
        public static async UniTask<T> FindBehaviourAsync<T>(this NetworkRunner runner, PlayerRef player, CancellationToken token) where T : SimulationBehaviour
        {
            while (token.IsCancellationRequested == false)
            {
                if (runner.FindBehaviour<T>(player, out var behaviour)) return behaviour;
                await UniTask.DelayFrame(1);
            }
            return default;
        }

#if !FUSION1
        public static void OnValueChanged<NB>(this ChangeDetector cd, NB nb, string n1, Action a1) where NB : NetworkBehaviour
            => cd.OnValueChanged(nb, (n1, a1));
        public static void OnValueChanged<NB>(this ChangeDetector cd, NB nb, string n1, Action a1, string n2, Action a2) where NB : NetworkBehaviour
            => cd.OnValueChanged(nb, (n1, a1), (n2, a2));
        public static void OnValueChanged<NB>(this ChangeDetector cd, NB nb, string n1, Action a1, string n2, Action a2, string n3, Action a3) where NB : NetworkBehaviour
            => cd.OnValueChanged(nb, (n1, a1), (n2, a2), (n3, a3));
        public static void OnValueChanged<NB>(this ChangeDetector cd, NB nb, string n1, Action a1, string n2, Action a2, string n3, Action a3, string n4, Action a4) where NB : NetworkBehaviour
            => cd.OnValueChanged(nb, (n1, a1), (n2, a2), (n3, a3), (n4, a4));
        public static void OnValueChanged<NB>(this ChangeDetector cd, NB nb, params (string name, Action action)[] action) where NB : NetworkBehaviour
        {
            foreach (var change in cd.DetectChanges(nb))
            {
                foreach (var item in action) if (item.name == change) item.action?.Invoke();
            }
        }

        public static void OnValueChanged<NB, T>(this ChangeDetector cd, NB nb, params (string name, Action<T, T> action)[] action) where NB : NetworkBehaviour where T : unmanaged
        {
            foreach (var change in cd.DetectChanges(nb, out var prevBuffer, out var currentBuffer))
            {
                foreach (var item in action)
                {
                    if (item.name == change)
                    {
                        var reader = GetPropertyReader<T>(typeof(T), item.name);
                        var (prev, current) = reader.Read(prevBuffer, currentBuffer);
                        item.action?.Invoke(prev, current);
                    }
                }
            }
        }
#else
        public static void LoadOld<T>(this Changed<T> changed, Action<T> old) where T : NetworkBehaviour
        {
            changed.LoadOld();
            old?.Invoke(changed.Behaviour);
            changed.LoadNew();
        }
        public static T2 LoadOld<T, T2>(this Changed<T> changed, Func<T, T2> old) where T : NetworkBehaviour
        {
            changed.LoadOld();
            var v = old.Invoke(changed.Behaviour);
            changed.LoadNew();
            return v;
        }
#endif

        #endregion

        #region PlayerRef

#if !FUSION1
        public static PlayerRef Host(this NetworkRunner runner) => runner.GameMode == GameMode.Server ? PlayerRef.None : PlayerRef.FromIndex(0);
#else
        public static PlayerRef Host(this NetworkRunner runner) => runner.GameMode == GameMode.Server ? PlayerRef.None : runner.Simulation.Config.DefaultPlayers - 1;
#endif
        public static bool IsServerMode(this NetworkRunner runner) => runner.GameMode == GameMode.Server;

        public static bool IsHost(this PlayerRef playerRef, NetworkRunner runner) => playerRef == Host(runner);
        public static bool IsMe(this PlayerRef playerRef, NetworkRunner runner) => playerRef == runner.LocalPlayer;

        /// <summary>
        /// Normally RpcInfo.Source will be None when Host/Server calls RPC.
        /// This extension method makes the Host's PlayerRef available when the Host calls an RPC.
        /// </summary>
        public static PlayerRef Source(this RpcInfo info, NetworkRunner runner) => info.Source.IsNone ? runner.Host() : info.Source;

        #endregion

        #region Other

        public static void Disconnects(this NetworkRunner runner, IEnumerable<PlayerRef> targetPlayers)
        {
            foreach (var p in targetPlayers) if (runner.ActivePlayers.Contains(p)) runner.Disconnect(p);
        }

        public static bool TryAssignInputAuthority(this NetworkObject no, Guid objectToken, bool noAssignment = true)
        {
            foreach (var p in no.Runner.ActivePlayers)
            {
                if (new Guid(no.Runner.GetPlayerConnectionToken(p)) != objectToken) continue;
                no.AssignInputAuthority(p);
                return true;
            }
            if (noAssignment) no.AssignInputAuthority(PlayerRef.None);
            return false;
        }

        #endregion
    }

    public static class PhotonFusionUtil
    {
        public static NetworkRunner Runner => NetworkRunner.Instances.FirstOrDefault(r => r != null);
        public static bool TryGetRunner(out NetworkRunner runner)
        {
            runner = Runner;
            return runner != null;
        }

        public static Dictionary<NetworkRunner, Dictionary<string, SessionProperty>> SingleSessionProperties;
        public static async void SetupSingleSessionProperties(this NetworkRunner runner, Dictionary<string, SessionProperty> props)
        {
            SingleSessionProperties ??= new();
            SingleSessionProperties.Add(runner, new());

            foreach (var pair in props)
            {
                if (SingleSessionProperties[runner].ContainsKey(pair.Key)) SingleSessionProperties[runner][pair.Key] = pair.Value;
                else SingleSessionProperties[runner].Add(pair.Key, pair.Value);
            }

            await runner.OnDestroyAsync();
            SingleSessionProperties.Remove(runner);
        }
        public static IReadOnlyDictionary<string, SessionProperty> GetCustomProperties(this NetworkRunner runner)
        {
            if (runner.GameMode == GameMode.Single) return SingleSessionProperties[runner];
            else return runner.SessionInfo.Properties;
        }
        public static SessionProperty GetCustomProperty(this NetworkRunner runner, string key)
        {
            if (runner.GameMode == GameMode.Single) return GetCustomProperties(runner)[key];
            return runner.SessionInfo.Properties[key];
        }
        public static bool UpdateCustomProperty(this NetworkRunner runner, string key, SessionProperty prop) => UpdateCustomProperties(runner, new() { { key, prop } });
        public static bool UpdateCustomProperties(this NetworkRunner runner, Dictionary<string, SessionProperty> props)
        {
            if (runner.GameMode == GameMode.Single)
            {
                var isUpdateAll = true;
                foreach (var prop in props)
                {
                    if (SingleSessionProperties[runner].ContainsKey(prop.Key)) SingleSessionProperties[runner][prop.Key] = SessionProperty.Convert(prop.Value);
                    else isUpdateAll = false;
                }
                return isUpdateAll;
            }
            return runner.SessionInfo.UpdateCustomProperties(props);
        }

        public static Observable<NetworkRunner> OnCreatedRunner(FrameProvider frameProvider, CancellationToken token)
            => Observable.EveryUpdate(frameProvider, NetworkRunner.Instances.FirstOrDefault().destroyCancellationToken)
                .Select(_ => NetworkRunner.Instances.FirstOrDefault()).Where(r => r != null).Take(1)
                .Publish().RefCount();
        public static Observable<NetworkRunner> OnBeganRunner(FrameProvider frameProvider, CancellationToken token)
            => Observable.EveryUpdate(frameProvider, NetworkRunner.Instances.FirstOrDefault().destroyCancellationToken)
                .Select(_ => NetworkRunner.Instances.FirstOrDefault()).Where(r => r != null && r.Tick > 0).Take(1)
                .Publish().RefCount();
        public static Observable<NetworkRunner> OnShotDownedRunner(FrameProvider frameProvider, CancellationToken token)
            => Observable.EveryUpdate(frameProvider, NetworkRunner.Instances.FirstOrDefault().destroyCancellationToken)
                .Select(_ => NetworkRunner.Instances.FirstOrDefault()).Where(r => r != null && r.IsShutdown).Take(1)
                .Publish().RefCount();
    }
}
