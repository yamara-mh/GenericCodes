using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.LowLevel;

namespace Audio
{
    public class SEPlayer : MonoBehaviour
    {
        private class AudioSourceData
        {
            public readonly AudioSource Source = null;
            public Transform Target = null;
            public Action<(AudioSource original, AudioSource instance)> Ended = null;
            public AudioSourceData(AudioSource source)
            {
                Source = source;
            }
        }
        private const int MaxPriority = byte.MaxValue + 1;

        public static SEPlayer Instance { get; private set; } = null;

        private static AudioSource _orininalAudioSource;
        private static List<AudioSourceData> _sourcesData = new();
        private static Dictionary<AudioMixerGroupEnum, AudioSource> _oneShotsSources = new();

        public static SEPlayerSettings Settings { get; private set; } = null;
        public static bool IsPausing { get; private set; } = false;
        public static int UsingCount { get; private set; } = 0;

        private static int _lowestPriority = MaxPriority;

        // Generate on awake
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void Generate()
        {
            var player = new GameObject().AddComponent<SEPlayer>();
            player.name = nameof(SEPlayer);
            Instance = player;
            DontDestroyOnLoad(Instance.gameObject);

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref playerLoop);
            Settings = await Addressables.LoadAssetAsync<SEPlayerSettings>(nameof(SEPlayerSettings));
            if (Settings == null) Debug.LogError(nameof(SEPlayerSettings) + " does not exist");

            _orininalAudioSource = Settings.InitialAudioSourcePrefab ? Settings.InitialAudioSourcePrefab : null;

            for (int i = 0; i < Settings.MaxAudioSource; i++) _sourcesData.Add(new(CreateAudioSource()));
        }

        private static AudioSource CreateAudioSource()
        {
            var audioSource = Settings.InitialAudioSourcePrefab ? Instantiate(Settings.InitialAudioSourcePrefab) : new GameObject(nameof(AudioSource)).AddComponent<AudioSource>();
            audioSource.transform.parent = Instance.transform;
            audioSource.outputAudioMixerGroup = Settings.InitialAudioMixerGroup;
            CleanAudioSource(audioSource);
            return audioSource;
        }
        private static AudioSource CreateOneShotAudioSource(AudioMixerGroupEnum groupEnum)
        {
            var source = Instance.gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = AudioMixerManager.GetMixerGroup(groupEnum);
            source.playOnAwake = false;
            source.priority = 0;
            source.reverbZoneMix = 0f;
            source.dopplerLevel = 0f;
            return source;
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
            source.volume = Settings.DefaultVolume;
            source.pitch = Settings.DefaultPitch;
            source.maxDistance = Settings.DefaultMaxDistance;
        }
        private static void CleanData(AudioSourceData data)
        {
            CleanAudioSource(data.Source);
            data.Ended?.Invoke((_orininalAudioSource, data.Source));
            data.Ended = null;
        }

        private static void GetAudioSourceProcess(AudioSourceData data, int priority, Transform transform, Action<(AudioSource original, AudioSource instance)> ended)
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
        }

        public static void PlayOneShot(AudioClip clip, AudioMixerGroupEnum mode = AudioMixerGroupEnum.Master, float volumeScale = 1f)
        {
            if (!_oneShotsSources.TryGetValue(mode, out _)) _oneShotsSources.Add(mode, CreateOneShotAudioSource(mode));
            _oneShotsSources[mode].PlayOneShot(clip, volumeScale);
        }

        public static AudioSource TryGetAudioSource(int priority = 0, Transform transform = null, Action<(AudioSource original, AudioSource instance)> ended = null)
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

        public static void Stop(bool audioSources = true, bool oneShotAll = true, params AudioMixerGroupEnum[] oneShotModes)
        {
            if (oneShotAll) foreach (var sources in _oneShotsSources.Values) sources.Stop();
            foreach (var mode in oneShotModes) _oneShotsSources[mode].Stop();
            if (!audioSources) return;

            foreach (var item in _sourcesData)
            {
                item.Source.Stop();
                CleanData(item);
            }
            UsingCount = 0;
            _lowestPriority = MaxPriority;
        }

        public static IEnumerable<AudioSource> GetAudioSourcesAll()
        {
            return _sourcesData.Select(d => d.Source);
        }
        public static void Pause(bool audioSources = true, bool oneShotAll = true, params AudioMixerGroupEnum[] oneShotModes)
        {
            if (oneShotAll) foreach (var sources in _oneShotsSources.Values) sources.Pause();
            foreach (var mode in oneShotModes) _oneShotsSources[mode].Pause();
            if (!audioSources) return;

            IsPausing = true;
            foreach (var item in _sourcesData) item.Source.Pause();
        }
        public static void UnPause(bool audioSources = true, bool oneShotAll = true, params AudioMixerGroupEnum[] oneShotModes)
        {
            if (oneShotAll) foreach (var sources in _oneShotsSources.Values) sources.UnPause();
            foreach (var mode in oneShotModes) _oneShotsSources[mode].UnPause();
            if (!audioSources) return;

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
                removeSource.Ended?.Invoke((_orininalAudioSource, removeSource.Source));
                removeSource.Ended = null;
            }
            _sourcesData.RemoveAll(a => removeSources.Any(r => r == a));
            Settings.MaxAudioSource = count;
        }
    }

    public static class SEManagerEx
    {
        public static AudioSource Play(this AudioClip clip, int priority = 0, Action<(AudioSource original, AudioSource instance)> ended = null, float delay = 0f)
        {
            var audioSource = SEPlayer.TryGetAudioSource(priority, null, ended);
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
            return audioSource;
        }
        public static AudioSource Play(this AudioClip clip, Transform transform, int priority = 0, Action<(AudioSource original, AudioSource instance)> ended = null, float delay = 0f)
        {
            var audioSource = SEPlayer.TryGetAudioSource(priority, transform, ended);
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
            return audioSource;
        }
        public static void PlayOneShot(this AudioClip clip, AudioMixerGroupEnum mode = AudioMixerGroupEnum.Master, float volumeScale = 1f)
            => SEPlayer.PlayOneShot(clip, mode, volumeScale);

        public static AudioSource SetPos(this AudioSource audioSource, Vector3 position, float spatialBlend = 1f)
        {
            audioSource.transform.position = position;
            audioSource.spatialBlend = spatialBlend;
            return audioSource;
        }
    }
}
