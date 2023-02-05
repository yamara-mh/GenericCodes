#define CLIENT_STATE_REPRODUCER__SPAWN_AFTER_SNAPSHOT

using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhotonFusionUtil
{
    /// <summary>
    /// By implementing this, the value of Client will be migrated during Host Migration.
    /// </summary>
    public interface IStateMigratable
    {
        public object GetState();
        public void SetState(object value);
        public object GetLocalState();
        public void SetLocalState(object value);

#if CLIENT_STATE_REPRODUCER__SPAWN_AFTER_SNAPSHOT
        /// <summary>
        /// Receive networkId change information.
        /// Server only runs.
        /// </summary>
        public void OnUpdatedNetworkId(NetworkId oldId, NetworkId newId);
#endif
    }

    public class ClientStateReproducer
    {
        private int _tick;

        private Dictionary<NetworkId, NetworkPrefabId> _prefabDict;

        private Dictionary<NetworkBehaviourId, object> _stateDict;
        private Dictionary<NetworkBehaviourId, object> _stateLocalDict;

        private Dictionary<NetworkId, (Vector3 pos, Rotation rot)> _transformDict;
        private Dictionary<NetworkId, (Vector3 velocity, Vector3 angularVelocity)> _rigidBodyDict;

        private int _capacity;

        public bool IsReproducible => _prefabDict != null && _prefabDict.Count > 0;
        public bool HasLocalState => _stateLocalDict != null && _stateLocalDict.Count > 0;

        public ClientStateReproducer(int capacity = 0)
        {
            _capacity = capacity;
        }

        /// <summary>
        /// Records information about NetworkObjects spawned by Spawn().
        /// Record Position, Rotation, Velocity, AngularVelocity by default.
        /// Otherwise, only record information for classes that implement IStateMigrate.
        /// </summary>
        public void RecordState(NetworkRunner runner, Func<NetworkObject, bool> recordConditions = null)
        {
            recordConditions ??= (no) => true;
            _tick = runner.Tick;

            _capacity = _capacity <= 0 ? runner.Config.MaxNetworkedObjectCount : _capacity;

            _prefabDict = new(_capacity);

            var networkObjectList = new List<NetworkObject>(_capacity);
            var networkId = new NetworkId();

            for (uint i = 0; i < runner.Config.MaxNetworkedObjectCount; i++)
            {
                networkId.Raw = i;
                var no = runner.FindObject(networkId);
                if (no != null && recordConditions.Invoke(no) && runner.Config.PrefabTable.TryGetId(no.NetworkGuid, out var id))
                {
                    _prefabDict.Add(no.Id, id);
                    networkObjectList.Add(no);
                }
            }

            _stateDict = new(_prefabDict.Count);
            _stateLocalDict = new(_prefabDict.Count);
            _transformDict = new(_prefabDict.Count);
            _rigidBodyDict = new(_prefabDict.Count);

            foreach (var NO in networkObjectList)
            {
                var position = Vector3.zero;
                var rotation = Quaternion.identity;

                if (NO.TryGetBehaviour<NetworkRigidbody>(out var rb))
                {
                    _rigidBodyDict.Add(NO.Id, (rb.ReadVelocity(), rb.ReadAngularVelocity()));
                }
#if CLIENT_STATE_REPRODUCER__PHYSICS_2D
                else if (NO.TryGetBehaviour<NetworkRigidbody2D>(out var rb2d))
                {
                    _rigidBodyDict.Add(NO.Id, (rb2d.ReadVelocity(), Vector3.forward * rb2d.ReadAngularVelocity()));
                }
#endif
                if (NO.TryGetBehaviour<NetworkPositionRotation>(out var posRot))
                {
                    position = posRot.ReadPosition();
                    rotation = posRot.ReadRotation();
                }
                else if (NO.TryGetBehaviour<NetworkPosition>(out var pos))
                {
                    position = pos.ReadPosition();
                }

                _transformDict.Add(NO.Id, (position, rotation));

                foreach (var NB in NO.NetworkedBehaviours)
                {
                    if (NB.TryGetComponent<IStateMigratable>(out var state))
                    {
                        _stateDict.Add(NB.Id, state.GetState());
                        _stateLocalDict.Add(NB.Id, state.GetLocalState());
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Respawn the NetworkObjects based on the recorded state.
        /// Can be executed only once per recording.
        /// </summary>
        /// <param name="forceMatchNetworkId">If you respawn a NetworkObject that is not recorded in Snapshot, NetworkId will be changed.
        /// Enabling this flag will force it to rewrite to the original NetworkId.
        /// May cause problems.</param>
        /// <param name="keepData">Enabling it will allow you to reproduce many times</param>
        public void ReproduceState(NetworkRunner runner,
            Action<NetworkRunner, NetworkObject> onBeforeSpawned = null,
            Action<NetworkRunner, NetworkObject> onAfterSpawned = null,
            bool forceMatchNetworkId = false)
        {
            while (runner.Tick != _tick) runner.Simulation.Update(runner.DeltaTime);

            var stateMigratables = new List<IStateMigratable>(_prefabDict.Count);

            foreach (var no in runner.GetResumeSnapshotNetworkObjects())
            {
                if (!_prefabDict.ContainsKey(no.Id)) continue;
                _prefabDict.Remove(no.Id);

                var networkObject = runner.Spawn(no, _transformDict[no.Id].pos, _transformDict[no.Id].rot,
                    onBeforeSpawned: (runner, NO) =>
                    {
                        OnBeforeSpawnedBase(NO.Id, NO, ref stateMigratables);
                        onBeforeSpawned?.Invoke(runner, NO);
                    });
                onAfterSpawned?.Invoke(runner, no);
            }

            var updateNetIdDict = new Dictionary<NetworkId, NetworkId>(_prefabDict.Count);

            // Spawns objects created after the PushHostMigrationSnapshot(). Note that the NetworkId will change
            foreach (var pair in _prefabDict)
            {
                var oldNetId = pair.Key;
                var prefabId = pair.Value;

                var networkObject = runner.Spawn(prefabId, _transformDict[oldNetId].pos, _transformDict[oldNetId].rot,
                    onBeforeSpawned: (runner, NO) =>
                    {
                        if (forceMatchNetworkId)
                        {
                            NO.Id = oldNetId;
                            OnBeforeSpawnedBase(oldNetId, NO, ref stateMigratables);
                        }
                        else
                        {
                            OnBeforeSpawnedBase(oldNetId, NO, ref stateMigratables);
                            updateNetIdDict.Add(oldNetId, NO.Id);
                        }
                        onBeforeSpawned?.Invoke(runner, NO);
                    });
                onAfterSpawned?.Invoke(runner, networkObject);
            }

#if CLIENT_STATE_REPRODUCER__SPAWN_AFTER_SNAPSHOT
            foreach (var state in stateMigratables)
            {
                foreach (var pair in updateNetIdDict) state.OnUpdatedNetworkId(pair.Key, pair.Value);
            }
#endif
        }
        private void OnBeforeSpawnedBase(NetworkId netId, NetworkObject no, ref List<IStateMigratable> stateMigratables)
        {
            foreach (var NB in no.NetworkedBehaviours)
            {
                if (_stateDict.ContainsKey(NB.Id) && NB.TryGetComponent<IStateMigratable>(out var state))
                {
                    state.SetState(_stateDict[NB.Id]);
                    stateMigratables.Add(state);
                }
            }

            if (no.TryGetBehaviour<NetworkRigidbody>(out var rb))
            {
                rb.WriteVelocity(_rigidBodyDict[netId].velocity);
                rb.WriteAngularVelocity(_rigidBodyDict[netId].angularVelocity);
            }
#if CLIENT_STATE_REPRODUCER__PHYSICS_2D
            else if (no.TryGetBehaviour<NetworkRigidbody2D>(out var rb2d))
            {
                rb2d.WriteVelocity(_rigidBodyDict[netId].velocity);
                rb2d.WriteAngularVelocity(_rigidBodyDict[netId].angularVelocity.z);
            }
#endif
        }

        /// <summary>
        /// Migrate local values
        /// </summary>
        public static void TryReproduceLocalState(NetworkRunner newRunner, ref NetworkRunner oldRunner)
        {
            if (oldRunner != null && oldRunner.Reproducer().HasLocalState)
            {
                oldRunner.Reproducer().ReproduceLocalState(newRunner, true);
                oldRunner.Reproducer().Clear();
                oldRunner.RemoveReproducer();
                oldRunner = null;
            }
        }
        /// <summary>
        /// Migrate local values
        /// </summary>
        public void ReproduceLocalState(NetworkRunner runner, bool clearOnCompleted = false)
        {
            var netId = new NetworkId();
            for (uint id = 0; id < runner.Config.MaxNetworkedObjectCount; id++)
            {
                netId.Raw = id;
                var no = runner.FindObject(netId);
                if (no == null) continue;

                foreach (var NB in no.NetworkedBehaviours)
                {
                    if (_stateDict.ContainsKey(NB.Id) && NB.TryGetComponent<IStateMigratable>(out var state))
                    {
                        state.SetLocalState(_stateLocalDict[NB.Id]);
                    }
                }
            }
            if (clearOnCompleted) Clear();
        }

        public void Clear(bool keepStateDict = false)
        {
            _prefabDict = null;
            _transformDict = null;
            _rigidBodyDict = null;
            if (!keepStateDict) _stateDict = null;
        }
    }

    public static class ClientStatesReproduceUtil
    {
        private static Dictionary<NetworkRunner, ClientStateReproducer> reproducerDict = new();
        public static ClientStateReproducer Reproducer(this NetworkRunner runner)
        {
            if (!reproducerDict.ContainsKey(runner)) reproducerDict.Add(runner, new());
            return reproducerDict[runner];
        }
        public static void RemoveReproducer(this NetworkRunner runner)
        {
            if (reproducerDict.ContainsKey(runner)) reproducerDict[runner].Clear();
            reproducerDict.Remove(runner);
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
    }
}
