using Fusion;
using System;
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
        public static int GetTickAfter(this NetworkRunner runner, float second) => runner.Tick + (int)(second / runner.DeltaTime);

        public static float ElapsedTime(this NetworkRunner runner, int tick) => (runner.Tick - tick) * runner.DeltaTime;
        public static float RemainingTime(this NetworkRunner runner, int tick) => -runner.ElapsedTime(tick);

        public static bool HasPassed(this NetworkRunner runner, int tick) => (runner.Tick - tick) >= 0;
        public static bool HasntPass(this NetworkRunner runner, int tick) => (runner.Tick - tick) < 0;

        #endregion

        #region TickTimer

        /// <summary>
        /// TickTimer が完了した時に実行
        /// </summary>
        public static IObservable<Unit> OnCompleted(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate()
                .Select(_ => tickTimer.Expired(runner))
                .DistinctUntilChanged()
                .Where(completed => completed)
                .Select(_ => Unit.Default)
                .Publish().RefCount();

        /// <summary>
        /// TickTimer が有効の間、常に実行
        /// </summary>
        public static IObservable<Unit> OnUpdated(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate().Where(_ => !tickTimer.Expired(runner)).Select(_ => Unit.Default)
                .Publish().RefCount();

        /// <summary>
        /// TickTimer の RemainingTime が更新された時に実行
        /// </summary>
        public static IObservable<float> OnRunnerUpdated(this TickTimer tickTimer, NetworkRunner runner)
            => Observable.EveryUpdate()
                .Where(_ => !tickTimer.Expired(runner))
                .Select(_ => tickTimer.RemainingTime(runner).Value)
                .DistinctUntilChanged()
                .Publish().RefCount();

        /// <summary>
        /// TickTimer の RemainingTime の整数部分が更新された時に実行
        /// </summary>
        public static IObservable<int> OnCountDowned(this TickTimer tickTimer, NetworkRunner runner, int minCount = 1, int maxCount = int.MaxValue)
            => Observable.EveryUpdate()
                .Where(_ => !tickTimer.Expired(runner))
                .Select(_ => Mathf.CeilToInt(tickTimer.RemainingTime(runner).Value))
                .DistinctUntilChanged()
                .Where(count => count >= minCount && count <= maxCount)
                .Publish().RefCount();

        #endregion

        #region Network Array, LinkedList, Dictionary

        public static int SetToEmpty<T>(this NetworkArray<T> array, Func<T, bool> emptyConditions, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (emptyConditions.Invoke(array.Get(i)))
                {
                    array.Set(i, value);
                    return i;
                }
            }
            return -1;
        }
        public static bool SetsEmpty<T>(this NetworkArray<T> array, Func<T, bool> emptyConditions, params T[] values)
        {
            var index = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (emptyConditions.Invoke(array.Get(i)))
                {
                    array.Set(i, values[index++]);
                    if (values.Length == index) return true;
                }
            }
            return false;
        }

        public static void ValueChanged<Class, T>(
            this NetworkArray<T> currentArray,
            Changed<Class> changed,
            Func<Class, NetworkArray<T>> loadArray,
            Action<int, T, T> action)
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

        public static void CopyFrom<T>(this NetworkArray<T> netArray, object obj)
        {
            var array = (T[])obj;
            netArray.CopyFrom(array, 0, array.Length - 1);
        }

        public static void CopyFrom<T>(this NetworkLinkedList<T> netList, object obj)
        {
            var array = (T[])obj;
            if (array.Length == netList.Count)
            {
                for (int i = 0; i < array.Length; i++) netList.Set(i, array[i]);
            }
            else
            {
                netList.Clear();
                for (int i = 0; i < array.Length; i++) netList.Add(array[i]);
            }
        }

        public static void CopyFrom<K, V>(this NetworkDictionary<K, V> netDict, object obj)
        {
            netDict.Clear();
            var array = (KeyValuePair<K, V>[])obj;
            for (int i = 0; i < array.Length; i++) netDict.Add(array[i].Key, array[i].Value);
        }

        #endregion

        #region Other

        public static void LoadOld<T>(this Changed<T> changed, Action<T> old) where T : NetworkBehaviour
        {
            changed.LoadOld();
            old?.Invoke(changed.Behaviour);
            changed.LoadNew();
        }

        public static bool TryAssignInputAuthority(this NetworkObject obj, NetworkRunner runner, Guid token, bool assignNone = true)
        {
            foreach (var p in runner.ActivePlayers)
            {
                var t = new Guid(runner.GetPlayerConnectionToken(p));
                if (t != token) continue;
                obj.AssignInputAuthority(p);
                return true;
            }
            if (assignNone) obj.AssignInputAuthority(PlayerRef.None);
            return false;
        }

        #endregion
    }
}
