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

namespace Yamara
{
    [DefaultExecutionOrder(-100)]
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; } = null;

        private Dictionary<string, ParticleData> _particles = new();
        public class ParticleData
        {
            public int UseCount;
            public ParticleSystem Instance;
            public AsyncOperationHandle<GameObject> Handle;

            public ParticleData(int useCount, ParticleSystem instance, AsyncOperationHandle<GameObject> handle)
            {
                UseCount = useCount;
                Instance = instance;
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
            if (Instance._particles.TryGetValue(particleRef.AssetGUID, out var p))
            {
                p.UseCount++;
                return p.Instance;
            }

            var handle = particleRef.LoadAssetAsync();
            var particle = Instantiate(await handle.Task, Instance.transform).GetComponent<ParticleSystem>();
            particle.Stop();
            var main = particle.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            Instance._particles.Add(particleRef.AssetGUID, new(1, particle, handle));
#if USE_UNIRX
            if (link) link.OnDestroyAsObservable().Subscribe(_ => RemoveOrDecrement(particleRef));
#endif
            return particle;
        }

        public static bool Contains(AssetReferenceT<GameObject> particleRef) => Contains(particleRef.AssetGUID);
        public static bool Contains(string AssetGUID) => Instance._particles.TryGetValue(AssetGUID, out _);

        public static void RemoveOrDecrement(AssetReferenceT<GameObject> particleRef) => RemoveOrDecrement(particleRef.AssetGUID);
        public static void RemoveOrDecrement(string AssetGUID)
        {
            if (!Instance._particles.TryGetValue(AssetGUID, out var particle)) return;
            if (--particle.UseCount > 0) return;
            ForceRemove(AssetGUID);
        }
        public static void ForceRemove(string AssetGUID)
        {
            if (!Instance._particles.TryGetValue(AssetGUID, out var particle)) return;
            Instance._particles.Remove(AssetGUID);
            Destroy(particle.Instance.gameObject);
            Addressables.Release(particle.Handle);
        }

        public static void RemoveAll()
        {
            foreach (var key in Instance._particles.Select(d => d.Key).ToArray()) ForceRemove(key);
        }

        public static ParticleSystem Play(AssetReferenceT<GameObject> particleRef, Vector3 position, Quaternion? quaternion = null, bool play = true)
        {
            if (Instance._particles.TryGetValue(particleRef.AssetGUID, out var particle))
            {
                Instance.PlayParticle(particle.Instance, position, quaternion, play);
                return particle.Instance;
            }
            Debug.LogError("Not added to ParticleManager : " + particleRef);
            return null;
        }
        private void PlayParticle(ParticleSystem particle, Vector3 position, Quaternion? quaternion = null, bool play = true)
        {
            quaternion ??= Quaternion.identity;
            particle.transform.SetPositionAndRotation(position, quaternion.Value);
            if (play) particle.Play();
        }
    }
}
