using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhotonFusionUtil
{
    public interface IStateMigratable
    {
        public object[] GetState();
        public void SetState(object[] values);
        public object[] GetLocalState();
        public void SetLocalState(object[] values);
    }

    public class ClientStateReproducer
    {
        public bool HasState => PrefabDict.Count > 0;

        private Dictionary<NetworkId, NetworkPrefabId> PrefabDict = new();
        private Dictionary<NetworkId, List<(object[] netValues, object[] localValues)>> StateDict = new();

        private Dictionary<NetworkId, (Vector3 pos, Rotation rot)> TransformDict = new();
        private Dictionary<NetworkId, (Vector3 velocity, Vector3 angularVelocity)> RigidBodyDict = new();

        private int Tick;

        public void RecordClientState(NetworkRunner runner, Func<NetworkObject, bool> recordConditions = null)
        {
            recordConditions ??= (no) => true;

            Tick = runner.Tick;

            var networkObjectList = new List<NetworkObject>();
            var networkId = new NetworkId();

            for (uint i = 0; i < runner.Config.MaxNetworkedObjectCount; i++)
            {
                networkId.Raw = i;
                var no = runner.FindObject(networkId);
                if (no != null && recordConditions.Invoke(no) && runner.Config.PrefabTable.TryGetId(no.NetworkGuid, out var id))
                {
                    PrefabDict.Add(no.Id, id);
                    networkObjectList.Add(no);
                }
            }

            foreach (var NO in networkObjectList)
            {
                var position = Vector3.zero;
                var rotation = Quaternion.identity;

                if (NO.TryGetBehaviour<NetworkRigidbody>(out var rb))
                {
                    RigidBodyDict.Add(NO.Id, (rb.ReadVelocity(), rb.ReadAngularVelocity()));
                }
                else if (NO.TryGetBehaviour<NetworkRigidbody2D>(out var rb2d))
                {
                    RigidBodyDict.Add(NO.Id, (rb2d.ReadVelocity(), Vector3.forward * rb2d.ReadAngularVelocity()));
                }

                if (NO.TryGetBehaviour<NetworkPositionRotation>(out var posRot))
                {
                    position = posRot.ReadPosition();
                    rotation = posRot.ReadRotation();
                }
                else if (NO.TryGetBehaviour<NetworkPosition>(out var pos))
                {
                    position = pos.ReadPosition();
                }

                TransformDict.Add(NO.Id, (position, rotation));

                foreach (var state in NO.GetComponents<IStateMigratable>())
                {
                    if (!StateDict.ContainsKey(NO.Id)) StateDict.Add(NO.Id, new());
                    StateDict[NO.Id].Add((state.GetState(), state.GetLocalState()));
                }
            }
        }

        public void ReproduceClientState(NetworkRunner runner,
            Action<NetworkRunner, NetworkObject> onBeforeSpawned = null,
            Action<NetworkRunner, NetworkObject> onAfterSpawned = null)
        {
            while (runner.Tick < Tick) runner.Simulation.Update(runner.DeltaTime);

            foreach (var pair in PrefabDict)
            {
                var netId = pair.Key;
                var prefabId = pair.Value;

                var networkObject = runner.Spawn(prefabId, TransformDict[netId].pos, TransformDict[netId].rot,
                    onBeforeSpawned: (runner, NO) =>
                    {
                        var states = NO.GetComponents<IStateMigratable>();
                        for (int i = 0; i < states.Length; i++) states[i].SetState(StateDict[netId][i].netValues);

                        if (NO.TryGetBehaviour<NetworkRigidbody>(out var rb))
                        {
                            rb.WriteVelocity(RigidBodyDict[netId].velocity);
                            rb.WriteAngularVelocity(RigidBodyDict[netId].angularVelocity);
                        }
                        else if (NO.TryGetBehaviour<NetworkRigidbody2D>(out var rb2d))
                        {
                            rb2d.WriteVelocity(RigidBodyDict[netId].velocity);
                            rb2d.WriteAngularVelocity(RigidBodyDict[netId].angularVelocity.z);
                        }

                        onBeforeSpawned?.Invoke(runner, NO);
                    });

                onAfterSpawned?.Invoke(runner, networkObject);
            }
        }

        public void ReproduceClientLocalState(NetworkRunner runner, bool clearOnCompleted = false)
        {
            var netId = new NetworkId();
            for (uint id = 0; id < runner.Config.MaxNetworkedObjectCount; id++)
            {
                netId.Raw = id;
                var no = runner.FindObject(netId);
                if (no == null || !StateDict.ContainsKey(netId)) continue;

                var states = no.GetComponents<IStateMigratable>();
                for (int i = 0; i < states.Length; i++) states[i].SetLocalState(StateDict[netId][i].localValues);
            }
            if (clearOnCompleted) Clear();
        }

        public void Clear()
        {
            PrefabDict.Clear();
            StateDict.Clear();
            TransformDict.Clear();
            RigidBodyDict.Clear();
        }
    }

    public static class ClientStatesReproduceUtil
    {
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
