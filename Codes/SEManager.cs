using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yamara
{
    public class SEManager : MonoBehaviour
    {
        private const int MaxPriority = byte.MaxValue + 1;

        public static SEManager Instance { get; private set; } = null;

        private class AudioSourceData
        {
            public readonly AudioSource Source = null;
            public Transform Target = null;
            public Action<AudioSource> Ended = null;
            public AudioSourceData(AudioSource source)
            {
                Source = source;
            }
        }
        private static List<AudioSourceData> _sourcesData = new();

        public static SEManagerSettings Settings { get; private set; } = null;
        public static bool IsPausing { get; private set; } = false;
        public static int UsingCount { get; private set; } = 0;

        private static int _lowestPriority = MaxPriority;

        // Generate SEManager on awake
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Generate()
        {
            var seManager = new GameObject().AddComponent<SEManager>();
            seManager.name = nameof(SEManager);
            Instance = seManager;
            DontDestroyOnLoad(Instance.gameObject);

            Settings = Resources.Load<SEManagerSettings>(nameof(SEManagerSettings));
            if (Settings == null) Debug.LogError(nameof(SEManagerSettings) + " does not exist in Resources");

            for (int i = 0; i < Settings.MaxAudioSource; i++) CreateAudioSource();
        }

        private static void CreateAudioSource()
        {
            var audioSource = new GameObject().AddComponent<AudioSource>();
            audioSource.name = nameof(AudioSource);
            audioSource.transform.SetParent(Instance.transform);
            CleanAudioSource(audioSource);

            _sourcesData.Add(new(audioSource));
        }

        private void LateUpdate()
        {
            if (IsPausing) return;
            foreach (var sourceData in _sourcesData)
            {
                if (sourceData.Source.isPlaying)
                {
                    if (sourceData.Target != null) sourceData.Source.transform.position = sourceData.Target.position;
                    continue;
                }
                else if (sourceData.Source.clip)
                {
                    CleanData(sourceData);
                    _lowestPriority = MaxPriority;
                    UsingCount--;
                }
            }
        }

        private static void CleanAudioSource(AudioSource source)
        {
            source.clip = null;
            source.priority = MaxPriority;
            source.pitch = 1f;
            source.outputAudioMixerGroup = Settings.DefaultAudioMixerGroup;
            source.volume = Settings.DefaultVolume;
            source.maxDistance = Settings.DefaultMaxDistance;
            source.spread = Settings.DefaultSpread;
            source.dopplerLevel = Settings.DefaultDopplerLevel;
        }
        private static void CleanData(AudioSourceData data)
        {
            CleanAudioSource(data.Source);
            data.Ended?.Invoke(data.Source);
            data.Ended = null;
        }

        public static AudioSource TryGetAudioSoure(int priority = 0, Transform transform = null, Action<AudioSource> ended = null)
        {
            if (_sourcesData.Count == 0)
            {
                Debug.LogError("No AudioSource available for play AudioClip. Please use " + nameof(ChangeAudioSourcesCount));
                return null;
            }
            if (priority > _lowestPriority) return null;

            if (UsingCount + 1 <= Settings.MaxAudioSource)
            {
                UsingCount++;
                var data = _sourcesData.First(a => a.Source.priority == MaxPriority);
                GetAudioSourceProcess(data, priority, transform, ended);
                return data.Source;
            }

            _lowestPriority = _sourcesData.First().Source.priority;
            var sourceData = _sourcesData.First();
            foreach (var data in _sourcesData.Skip(1))
            {
                if (data.Source.priority > _lowestPriority)
                {
                    _lowestPriority = data.Source.priority;
                    sourceData = data;
                }
            }
            GetAudioSourceProcess(sourceData, priority, transform, ended);
            return sourceData.Source;
        }

        private static void GetAudioSourceProcess(AudioSourceData data, int priority, Transform transform, Action<AudioSource> ended)
        {
            data.Source.priority = priority;
            data.Target = transform;
            data.Ended = ended;

            if (transform)
            {
                data.Source.spatialBlend = 1f;
                data.Source.transform.position = transform.position;
            }
            else data.Source.spatialBlend = 0f;

            if (IsPausing) data.Source.Pause();
        }

        public static void Stop()
        {
            foreach (var item in _sourcesData)
            {
                item.Source.Stop();
                CleanData(item);
            }
            UsingCount = 0;
            _lowestPriority = MaxPriority;
        }

        public static IEnumerable<AudioSource> GetAudioSources => _sourcesData.Select(d => d.Source);
        public static void Pause()
        {
            IsPausing = true;
            foreach (var item in _sourcesData) item.Source.Pause();
        }
        public static void UnPause()
        {
            IsPausing = false;
            foreach (var item in _sourcesData) item.Source.UnPause();
        }

        public static void ChangeAudioSourcesCount(int count)
        {
            count = Mathf.Max(0, count);
            if (count >= Settings.MaxAudioSource)
            {
                for (int i = Settings.MaxAudioSource; i < count; i++) CreateAudioSource();
                Settings.MaxAudioSource = count;
                return;
            }

            var removeSources = _sourcesData
                .OrderBy(a => !a.Source.isPlaying)
                .ThenByDescending(a => a.Source.priority)
                .Take(Settings.MaxAudioSource - count);

            foreach (var removeSource in removeSources)
            {
                removeSource.Ended?.Invoke(removeSource.Source);
                removeSource.Ended = null;
            }
            _sourcesData.RemoveAll(a => removeSources.Any(r => r == a));
            Settings.MaxAudioSource = count;
        }
    }

    public static class SEManagerEx
    {
        public static AudioSource Play(this AudioClip clip, int priority = 0, Action<AudioSource> ended = null, float delay = 0f)
        {
            var audioSource = SEManager.TryGetAudioSoure(priority, null, ended);
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
            return audioSource;
        }
        public static AudioSource Play(this AudioClip clip, Transform transform, int priority = 0, Action<AudioSource> ended = null, float delay = 0f)
        {
            var audioSource = SEManager.TryGetAudioSoure(priority, transform, ended);
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
            return audioSource;
        }

        public static AudioSource SetPos(this AudioSource audioSource, Vector3 position, float spatialBlend = 1f)
        {
            audioSource.transform.position = position;
            audioSource.spatialBlend = spatialBlend;
            return audioSource;
        }
    }
}
