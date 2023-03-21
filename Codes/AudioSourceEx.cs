using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Yamara
{
    [CreateAssetMenu(fileName = nameof(SEManagerSettings), menuName = nameof(ScriptableObject) + "/Create " + nameof(SEManagerSettings))]
    public class SEManagerSettings : ScriptableObject
    {
        [SerializeField] public int AudioSourceCount = 8;

        [SerializeField] public AudioMixerGroup AudioMixerGroup;

        [SerializeField, Range(0f, 1f)] public float DefaultVolume = 1f;

        [SerializeField] public float DefaultMaxDistance = 50f;

        [SerializeField, Range(0f, 360f)] public float Spread = 30f;

        [SerializeField, Range(0f, 5f)] public float DopplerLevel = 1f;
    }

    public class SEManager : MonoBehaviour
    {
        private const int MaxPriority = byte.MaxValue + 1;

        public static SEManager Instance { get; private set; } = null;

        private class AudioSourceData
        {
            public readonly AudioSource Source = null;
            public Transform Target = null;
            public Action<AudioSource> Completed = null;
            public AudioSourceData(AudioSource source)
            {
                Source = source;
            }
        }
        private static List<AudioSourceData> _sourcesData = new();

        public static SEManagerSettings Settings { get; private set; } = null;
        public static bool IsPausing { get; private set; } = false;

        private static int _lowestPriority = MaxPriority;
        private static int _playingCount = 0;

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

            for (int i = 0; i < Settings.AudioSourceCount; i++) CreateAudioSource();
        }

        private static void CreateAudioSource()
        {
            var audioSource = new GameObject().AddComponent<AudioSource>();
            audioSource.name = nameof(AudioSource);
            audioSource.transform.SetParent(Instance.transform);

            audioSource.outputAudioMixerGroup = Settings.AudioMixerGroup;
            audioSource.playOnAwake = false;
            audioSource.priority = MaxPriority;
            audioSource.spread = Settings.Spread;
            audioSource.dopplerLevel = Settings.DopplerLevel;
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
                    RefreshData(sourceData);
                    _lowestPriority = MaxPriority;
                    _playingCount--;
                }
            }
        }

        private static void RefreshData(AudioSourceData data)
        {
            var source = data.Source;
            source.priority = MaxPriority;
            source.clip = null;
            source.volume = Settings.DefaultVolume;
            source.pitch = 1f;
            source.maxDistance = Settings.DefaultMaxDistance;

            data.Completed?.Invoke(source);
            data.Completed = null;
        }


        public static AudioSource Play(AudioClip clip, Action<AudioSource> completed = null, float delay = 0f)
            => TryPlayProcess(clip, 0, null, null, completed, delay);
        public static AudioSource Play(AudioClip clip, Vector3 position, Action<AudioSource> completed = null, float delay = 0f)
            => TryPlayProcess(clip, 0, position, null, completed, delay);
        public static AudioSource Play(AudioClip clip, Transform transform, Action<AudioSource> completed = null, float delay = 0f)
            => TryPlayProcess(clip, 0, null, transform, completed, delay);

        public static AudioSource TryPlay(AudioClip clip, int priority, Action<AudioSource> completed = null, float delay = 0f)
                => TryPlayProcess(clip, priority, null, null, completed, delay);
        public static AudioSource TryPlay(AudioClip clip, int priority, Vector3 position, Action<AudioSource> completed = null, float delay = 0f)
            => TryPlayProcess(clip, priority, position, null, completed, delay);
        public static AudioSource TryPlay(AudioClip clip, int priority, Transform transform, Action<AudioSource> completed = null, float delay = 0f)
            => TryPlayProcess(clip, priority, null, transform, completed, delay);

        private static AudioSource TryPlayProcess(AudioClip clip, int priority = 0, Vector3? position = null, Transform transform = null, Action<AudioSource> completed = null, float delay = 0f)
        {
            if (_sourcesData.Count == 0)
            {
                Debug.LogError("No AudioSource available for play AudioClip. Please use " + nameof(ChangeAudioSourcesCount));
                return null;
            }
            if (priority > _lowestPriority) return null;

            if (_playingCount + 1 <= Settings.AudioSourceCount)
            {
                _playingCount++;
                var data = _sourcesData.First(a => a.Source.priority == MaxPriority);
                PlayProcess(data, clip, priority, position, transform, completed, delay);
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
            PlayProcess(sourceData, clip, priority, position, transform, completed, delay);
            return sourceData.Source;
        }

        private static void PlayProcess(AudioSourceData data, AudioClip clip, int priority, Vector3? pos, Transform transform, Action<AudioSource> completed, float delay)
        {
            data.Source.clip = clip;
            data.Source.priority = priority;
            data.Target = transform;
            data.Completed = completed;

            if (transform)
            {
                data.Source.spatialBlend = 1f;
                data.Source.transform.position = transform.position;
            }
            else if (pos.HasValue)
            {
                data.Source.spatialBlend = 1f;
                data.Source.transform.position = pos.Value;
            }
            else data.Source.spatialBlend = 0f;

            data.Source.PlayDelayed(delay);
            if (IsPausing) data.Source.Pause();
        }

        public static void Stop()
        {
            foreach (var item in _sourcesData)
            {
                item.Source.Stop();
                RefreshData(item);
            }
            _playingCount = 0;
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
            if (count >= Settings.AudioSourceCount)
            {
                for (int i = Settings.AudioSourceCount; i < count; i++) CreateAudioSource();
                Settings.AudioSourceCount = count;
                return;
            }

            var removeSources = _sourcesData
                .OrderBy(a => !a.Source.isPlaying)
                .ThenByDescending(a => a.Source.priority)
                .Take(Settings.AudioSourceCount - count);

            foreach (var removeSource in removeSources)
            {
                removeSource.Completed?.Invoke(removeSource.Source);
                removeSource.Completed = null;
            }
            _sourcesData.RemoveAll(a => removeSources.Any(r => r == a));
            Settings.AudioSourceCount = count;
        }
    }

    public static class SEManagerEx
    {
        public static AudioSource Play(this AudioClip clip, Action<AudioSource> completed = null, float delay = 0f)
            => SEManager.Play(clip, completed, delay);
        public static AudioSource Play(this AudioClip clip, Vector3 position, Action<AudioSource> completed = null, float delay = 0f)
            => SEManager.Play(clip, position, completed, delay);
        public static AudioSource Play(this AudioClip clip, Transform transform, Action<AudioSource> completed = null, float delay = 0f)
            => SEManager.Play(clip, transform, completed, delay);

        public static AudioSource TryPlay(this AudioClip clip, int priority, Action<AudioSource> completed = null, float delay = 0f)
            => SEManager.TryPlay(clip, priority, completed, delay);
        public static AudioSource TryPlay(this AudioClip clip, int priority, Vector3 position, Action<AudioSource> completed = null, float delay = 0f)
            => SEManager.TryPlay(clip, priority, position, completed, delay);
        public static AudioSource TryPlay(this AudioClip clip, int priority, Transform transform, Action<AudioSource> completed = null, float delay = 0f)
            => SEManager.TryPlay(clip, priority, transform, completed, delay);
    }
}
