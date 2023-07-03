using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using Cysharp.Threading.Tasks;
// using UnityEngine.AddressableAssets;

namespace Yamara
{
    public class SEManager : MonoBehaviour
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

        public static SEManager Instance { get; private set; } = null;

        private static AudioSource _orininalAudioSource;
        private static List<AudioSourceData> _sourcesData = new();
        private static Dictionary<AudioMixerMode, AudioSource> _oneShotsSources = new();

        public static SEManagerSettings Settings { get; private set; } = null;
        public static bool IsPausing { get; private set; } = false;
        public static int UsingCount { get; private set; } = 0;

        private static int _lowestPriority = MaxPriority;

        // Generate SEManager on awake
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Generate()
        {
            var seManager = new GameObject().AddComponent<SEManager>();
            seManager.name = nameof(SEManager);
            Instance = seManager;
            DontDestroyOnLoad(Instance.gameObject);

            Settings = Resources.Load<SEManagerSettings>(nameof(SEManagerSettings));
            // Settings = await Addressables.LoadAssetAsync<SEManagerSettings>(nameof(SEManagerSettings));
            if (Settings == null) Debug.LogError(nameof(SEManagerSettings) + " does not exist");

            _orininalAudioSource = Settings.DefaultAudioSourcePrefab ? Settings.DefaultAudioSourcePrefab : null;

            for (int i = 0; i < Settings.MaxAudioSource; i++) _sourcesData.Add(new(CreateAudioSource()));

            foreach (var settings in Settings.AudioMixerGroupSettings)
            {
                _oneShotsSources.Add(settings.Mode, CreateOneShotAudioSource(settings));
            }
            if (_oneShotsSources.Count == 0)
            {
                _oneShotsSources.Add(AudioMixerMode.Default, CreateOneShotAudioSource(new()
                {
                    Mode = AudioMixerMode.Default,
                    OneShotPriority = 0,
                }));
            }
        }

        private static AudioSource CreateAudioSource()
        {
            var audioSource = Settings.DefaultAudioSourcePrefab ? Instantiate(Settings.DefaultAudioSourcePrefab) : new GameObject(nameof(AudioSource)).AddComponent<AudioSource>();
            audioSource.transform.parent = Instance.transform;
            CleanAudioSource(audioSource);
            return audioSource;
        }
        private static AudioSource CreateOneShotAudioSource(SEManagerSettings.AudioMixerSetting settings)
        {
            var source = Instance.gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = Settings.AudioMixerGroupSettings.FirstOrDefault(m => m.Mode == AudioMixerMode.Default).Output;
            source.playOnAwake = false;
            source.priority = settings.OneShotPriority;
            source.reverbZoneMix = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        private static void CleanAudioSource(AudioSource source)
        {
            source.clip = null;
            source.priority = MaxPriority;

            source.outputAudioMixerGroup = Settings.output;
            source.bypassEffects = Settings.bypassEffects;
            source.bypassListenerEffects = Settings.bypassListenerEffects;
            source.bypassReverbZones = Settings.bypassReverbZones;
            source.volume = Settings.volume;
            source.pitch = Settings.pitch;
            source.panStereo = Settings.panStereo;
            source.spatialBlend = Settings.spatialBlend;
            source.reverbZoneMix = Settings.reverbZoneMix;
            source.dopplerLevel = Settings.dopplerLevel;
            source.spread = Settings.spread;
            source.minDistance = Settings.minDistance;
            source.maxDistance = Settings.maxDistance;
        }
        private static void CleanData(AudioSourceData data)
        {
            CleanAudioSource(data.Source);
            data.Ended?.Invoke((_orininalAudioSource, data.Source));
            data.Ended = null;
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

        private static void GetAudioSourceProcess(ref AudioSourceData data, int priority, Transform transform, Action<(AudioSource original, AudioSource instance)> ended)
        {
            data.Source.priority = priority;
            data.Target = transform;
            data.Ended = ended;

            if (transform)
            {
                data.Source.spatialBlend = Settings.spatialBlend;
                data.Source.transform.position = transform.position;
            }
            else data.Source.spatialBlend = 0f;
        }

        public static void PlayOneShot(AudioClip clip, AudioMixerMode mode = AudioMixerMode.Default, float volumeScale = 1f)
        {
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
                var data = _sourcesData.FirstOrDefault(a => a.Source.priority == MaxPriority);
                GetAudioSourceProcess(ref data, priority, transform, ended);
                return data.Source;
            }

            _lowestPriority = _sourcesData.FirstOrDefault().Source.priority;
            var sourceData = _sourcesData.FirstOrDefault();
            foreach (var data in _sourcesData.Skip(1))
            {
                if (data.Source.priority > _lowestPriority)
                {
                    _lowestPriority = data.Source.priority;
                    sourceData = data;
                }
            }
            GetAudioSourceProcess(ref sourceData, priority, transform, ended);
            return sourceData.Source;
        }

        public static void Stop(bool audioSources = true, bool oneShotAll = true, params AudioMixerMode[] oneShotModes)
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
        public static void Pause(bool audioSources = true, bool oneShotAll = true, params AudioMixerMode[] oneShotModes)
        {
            if (oneShotAll) foreach (var sources in _oneShotsSources.Values) sources.Pause();
            foreach (var mode in oneShotModes) _oneShotsSources[mode].Pause();
            if (!audioSources) return;

            IsPausing = true;
            foreach (var item in _sourcesData) item.Source.Pause();
        }
        public static void UnPause(bool audioSources = true, bool oneShotAll = true, params AudioMixerMode[] oneShotModes)
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
            var audioSource = SEManager.TryGetAudioSource(priority, null, ended);
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
            return audioSource;
        }
        public static AudioSource Play(this AudioClip clip, Transform transform, int priority = 0, Action<(AudioSource original, AudioSource instance)> ended = null, float delay = 0f)
        {
            var audioSource = SEManager.TryGetAudioSource(priority, transform, ended);
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
            return audioSource;
        }
        public static void PlayOneShot(this AudioClip clip, AudioMixerMode mode = AudioMixerMode.Default, float volumeScale = 1f)
            => SEManager.PlayOneShot(clip, mode, volumeScale);

        public static AudioSource SetPos(this AudioSource audioSource, Vector3 position, float spatialBlend = 1f)
        {
            audioSource.transform.position = position;
            audioSource.spatialBlend = spatialBlend;
            return audioSource;
        }

        public static AudioSource SetOutput(this AudioSource audioSource, AudioMixerMode mixer)
        {
            audioSource.outputAudioMixerGroup = SEManager.Settings.GetAudioMixerGroup(mixer);
            return audioSource;
        }
    }
}
