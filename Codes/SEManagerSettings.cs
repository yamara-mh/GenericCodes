using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Yamara
{
    public enum AudioMixerMode
    {
        Default = 0,
        Master = 1,
        UI = 2,
        SE = 3,
        Voice = 4,
    }

    [CreateAssetMenu(fileName = nameof(SEManagerSettings), menuName = nameof(ScriptableObject) + "/Create " + nameof(SEManagerSettings))]
    public class SEManagerSettings : ScriptableObject
    {
        [Serializable]
        public class AudioMixerSetting
        {
            [SerializeField] public AudioMixerMode Mode;
            [SerializeField] public AudioMixerGroup Output;
            [SerializeField, Range(0, 256)] public int OneShotPriority = 128;
        }
        [SerializeField] public List<AudioMixerSetting> AudioMixerGroupSettings = new();

        public AudioMixerGroup GetAudioMixerGroup(AudioMixerMode mode) => AudioMixerGroupSettings.FirstOrDefault(m => m.Mode == mode).Output;

        [SerializeField]
        public int MaxAudioSource = 8;

        [Header("Option")]
        [SerializeField, Tooltip("If not specified, the default settings will be used")]
        public AudioSource DefaultAudioSourcePrefab = null;

        [Header("Clean Properties (Add/Remove as desired)")]
        [SerializeField] public AudioMixerGroup output;
        [SerializeField] public bool bypassEffects;
        [SerializeField] public bool bypassListenerEffects;
        [SerializeField] public bool bypassReverbZones;
        [SerializeField, Range(0f, 1f)] public float volume = 1f;
        [SerializeField, Range(0f, 3f)] public float pitch = 1f;
        [SerializeField, Range(-1f, 1f)] public float panStereo = 0f;
        [SerializeField, Range(0f, 1f)] public float spatialBlend = 0f;
        [SerializeField, Range(0f, 1.1f)] public float reverbZoneMix = 0f;
        [SerializeField, Range(0f, 5f)] public float dopplerLevel = 0f;
        [SerializeField, Range(0f, 360f)] public float spread = 45f;
        [SerializeField] public AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
        [SerializeField] public float minDistance = 1f;
        [SerializeField] public float maxDistance = 50f;
    }
}
