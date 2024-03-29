// #define USE_UNIRX

#if USE_UNIRX
using UniRx;
using UniRx.Triggers;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Particle
{
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; } = null;

        private Dictionary<string, ParticleData> _data = new();
        private class ParticleData
        {
            public int UseCount;
            public ParticleSystem Particle;
            public AsyncOperationHandle<GameObject> Handle;

            public ParticleData(int useCount, ParticleSystem particle, AsyncOperationHandle<GameObject> handle)
            {
                UseCount = useCount;
                Particle = particle;
                Handle = handle;
            }
        }

        // Generate ParticleManager on awake
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Generate()
        {
            var manager = new GameObject().AddComponent<ParticleManager>();
            manager.name = nameof(ParticleManager);
            Instance = manager;
            DontDestroyOnLoad(Instance.gameObject);
        }

        private void OnDestroy() => RemoveAll();

        public static async Task<ParticleSystem> AddOrIncrementAsync(AssetReferenceT<GameObject> particleRef
#if USE_UNIRX
            , Component link
#endif
            )
        {
            if (Instance._data.TryGetValue(particleRef.AssetGUID, out var p))
            {
                p.UseCount++;
#if USE_UNIRX
                if (link) link.OnDestroyAsObservable().Subscribe(_ => RemoveOrDecrement(particleRef));
#endif
                return p.Particle;
            }

            Instance._data.Add(particleRef.AssetGUID, new(1, null, default));

            var handle = particleRef.LoadAssetAsync();
            var particle = Instantiate(await handle.Task, Instance.transform).GetComponent<ParticleSystem>();
            particle.Stop();
            var main = particle.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            Instance._data[particleRef.AssetGUID].Particle = particle;
            Instance._data[particleRef.AssetGUID].Handle = handle;
#if USE_UNIRX
             if (link) link.OnDestroyAsObservable().Subscribe(_ => RemoveOrDecrement(particleRef));
#endif
            return particle;
        }

        public static bool Contains(AssetReferenceT<GameObject> particleRef) => Contains(particleRef.AssetGUID);
        public static bool Contains(string AssetGUID) => Instance._data.TryGetValue(AssetGUID, out _);

        public static void RemoveOrDecrement(AssetReferenceT<GameObject> particleRef) => RemoveOrDecrement(particleRef.AssetGUID);
        public static void RemoveOrDecrement(string AssetGUID)
        {
            if (!Instance._data.TryGetValue(AssetGUID, out var particle)) return;
            if (--particle.UseCount > 0) return;
            ForceRemove(AssetGUID);
        }
        public static void ForceRemove(string AssetGUID)
        {
            if (!Instance._data.TryGetValue(AssetGUID, out var particle)) return;
            Instance._data.Remove(AssetGUID);
            Destroy(particle.Particle.gameObject);
            Addressables.Release(particle.Handle);
        }
        public static void RemoveAll()
        {
            foreach (var key in Instance._data.Select(d => d.Key).ToArray()) ForceRemove(key);
        }

        public static ParticleSystem Get(AssetReferenceT<GameObject> particleRef)
        {
            if (Instance._data.TryGetValue(particleRef.AssetGUID, out var particle)) return particle.Particle;
            Debug.LogError("Not added to ParticleManager : " + particleRef);
            return null;
        }
        public static ParticleSystem Get(AssetReferenceT<GameObject> particleRef, Vector3 position, Quaternion? quaternion = null)
        {
            if (Instance._data.TryGetValue(particleRef.AssetGUID, out var particle))
            {
                quaternion ??= Quaternion.identity;
                particle.Particle.transform.SetPositionAndRotation(position, quaternion.Value);
                return particle.Particle;
            }
            Debug.LogError("Not added to ParticleManager : " + particleRef);
            return null;
        }
        public static IEnumerable<ParticleSystem> GetAll() => Instance._data.Values.Select(d => d.Particle);
    }
}
