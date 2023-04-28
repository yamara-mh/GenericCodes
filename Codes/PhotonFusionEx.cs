using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Extensions
{
    public static class PhotonFusionEx
    {
        #region Tick

        public static int ToTick(this int second, NetworkRunner runner) => (int)(second / runner.DeltaTime);
        public static int ToTick(this float second, NetworkRunner runner) => (int)(second / runner.DeltaTime);
        public static float ToSecond(this int tick, NetworkRunner runner) => tick * runner.DeltaTime;

        public static int GetTickAfter(this NetworkRunner runner, float second) => runner.Tick + (int)(second / runner.DeltaTime);

        public static float ElapsedTime(this NetworkRunner runner, int tick) => (runner.Tick - tick) * runner.DeltaTime;
        public static float RemainingTime(this NetworkRunner runner, int tick) => -runner.ElapsedTime(tick);

        public static bool HasPassed(this NetworkRunner runner, int tick) => (runner.Tick - tick) >= 0;
        public static bool HasntPass(this NetworkRunner runner, int tick) => (runner.Tick - tick) < 0;
        #endregion

        #region TickTimer

        public static IObservable<Unit> OnCompleted(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate()
                .Select(_ => tickTimer.Expired(runner))
                .DistinctUntilChanged()
                .Where(completed => completed)
                .Select(_ => Unit.Default)
                .Publish().RefCount();

        public static IObservable<Unit> OnUpdated(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate().Where(_ => !tickTimer.Expired(runner)).Select(_ => Unit.Default)
                .Publish().RefCount();

        public static IObservable<float> OnRunnerUpdated(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate()
                .Where(_ => !tickTimer.Expired(runner))
                .Select(_ => tickTimer.RemainingTime(runner).Value)
                .DistinctUntilChanged()
                .Publish().RefCount();

        public static IObservable<int> OnCountDowned(this TickTimer tickTimer, NetworkRunner runner, int minCount = 1, int maxCount = int.MaxValue)
            => Observable.EveryUpdate()
                .Where(_ => !tickTimer.Expired(runner))
                .Select(_ => Mathf.CeilToInt(tickTimer.RemainingTime(runner).Value))
                .DistinctUntilChanged()
                .Where(count => count >= minCount && count <= maxCount)
                .Publish().RefCount();

        #endregion

        #region Network Array, LinkedList, Dictionary

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

        public static void CopyFrom<T>(this NetworkArray<T> netArray, T[] array) => netArray.CopyFrom(array, 0, array.Length - 1);

        #endregion

        #region Network LinkedList, Dictionary

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

        #region Other
        public static PlayerRef Host(this NetworkRunner runner) => runner.GameMode == GameMode.Server ? PlayerRef.None : runner.Simulation.MaxConnections;
        public static bool IsServerMode(this NetworkRunner runner) => runner.GameMode == GameMode.Server;

        public static bool IsHost(this PlayerRef playerRef, NetworkRunner runner) => playerRef == runner.Simulation.MaxConnections;
        public static bool IsMe(this PlayerRef playerRef, NetworkRunner runner) => playerRef == runner.LocalPlayer;
        public static bool HasInputAuthorityTo(this PlayerRef playerRef, NetworkObject no) => playerRef == no.InputAuthority;
        public static bool HasStateAuthorityTo(this PlayerRef playerRef, NetworkObject no) => playerRef == no.StateAuthority;
        public static bool HasInputAuthorityTo(this PlayerRef playerRef, NetworkBehaviour nb) => playerRef == nb.Object.InputAuthority;
        public static bool HasStateAuthorityTo(this PlayerRef playerRef, NetworkBehaviour nb) => playerRef == nb.Object.StateAuthority;

        public static PlayerRef Source(this RpcInfo info, NetworkRunner runner) => info.Source.IsNone ? runner.Host() : info.Source;

        public static int GetSeed(this NetworkBehaviour nb) => unchecked((int)nb.Runner.SessionInfo.Properties["seed"] + nb.Id.Behaviour);

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

        public static bool TryAssignInputAuthority(this NetworkObject obj, NetworkRunner runner, Guid token, bool noAssignment = true)
        {
            foreach (var p in runner.ActivePlayers)
            {
                if (new Guid(runner.GetPlayerConnectionToken(p)) != token) continue;
                obj.AssignInputAuthority(p);
                return true;
            }
            if (noAssignment) obj.AssignInputAuthority(PlayerRef.None);
            return false;
        }

        #endregion
    }

    public static class PhotonFusionUtil
    {
        public static NetworkRunner Runner => NetworkRunner.Instances.FirstOrDefault();
    }
}
