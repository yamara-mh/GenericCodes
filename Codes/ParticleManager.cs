using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Yamara
{
    public class ParticleManagerData
    {
        public int Count;
        public ParticleSystem Instance;
        public AsyncOperationHandle<GameObject> Handle;

        public ParticleManagerData(int count, ParticleSystem instance, AsyncOperationHandle<GameObject> handle)
        {
            Count = count;
            Instance = instance;
            Handle = handle;
        }
    }

    [DefaultExecutionOrder(-100)]
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; } = null;

        private Dictionary<string, ParticleManagerData> _particles = new();

        // Generate ParticleManager on awake
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Generate()
        {
            var manager = new GameObject().AddComponent<ParticleManager>();
            manager.name = "ParticleManager";
            Instance = manager;
            DontDestroyOnLoad(Instance.gameObject);
        }

        private void OnDestroy() => RemoveAll();

        public static async void AddOrIncrement(AssetReferenceT<GameObject> particleRef)
        {
            if (Instance._particles.TryGetValue(particleRef.AssetGUID, out var p))
            {
                p.Count++;
                return;
            }

            var handle = particleRef.LoadAssetAsync();
            var particle = Instantiate(await handle.Task, Instance.transform).GetComponent<ParticleSystem>();
            particle.Stop();
            var main = particle.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            Instance._particles.Add(particleRef.AssetGUID, new(1, particle, handle));
        }

        public static bool Contains(AssetReferenceT<GameObject> particleRef) => Contains(particleRef.AssetGUID);
        public static bool Contains(string AssetGUID) => Instance._particles.TryGetValue(AssetGUID, out _);

        public static void RemoveOrDecrement(AssetReferenceT<GameObject> particleRef, bool force = false) => RemoveOrDecrement(particleRef.AssetGUID, force);
        public static void RemoveOrDecrement(string AssetGUID, bool force = false)
        {
            if (!Instance._particles.TryGetValue(AssetGUID, out var particle)) return;
            if (--particle.Count > 0 || force) return;

            Instance._particles.Remove(AssetGUID);
            Destroy(particle.Instance.gameObject);
            Addressables.Release(particle.Handle);
        }
        public static void RemoveAll()
        {
            foreach (var key in Instance._particles.Select(d => d.Key).ToArray()) RemoveOrDecrement(key, true);
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
