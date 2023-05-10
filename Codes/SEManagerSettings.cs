using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yamara
{
    public enum OneShotMode
    {
        Default = 0,
        UI = 1,
        SE = 2,
        Voice = 3,
    }

    [CreateAssetMenu(fileName = nameof(SEManagerSettings), menuName = nameof(ScriptableObject) + "/Create " + nameof(SEManagerSettings))]
    public class SEManagerSettings : ScriptableObject
    {
        [Serializable]
        public class OneShotAudioSource
        {
            public OneShotMode Type;
            public AudioMixerGroup Output;
            [SerializeField, Range(0, 256)] public int Priority = 0;
        }
        [SerializeField] public List<OneShotAudioSource> OneShotAudioSourcesSettings = new ();

        [SerializeField] public AudioSource AudioSourcePrefab = null;
        [SerializeField] public int MaxAudioSource = 8;

        [SerializeField, Range(0f, 1f)] public float DefaultVolume = 1f;
        [SerializeField, Range(0f, 1f)] public float DefaultPitch = 1f;
        [SerializeField] public float DefaultMaxDistance = 50f;
    }
}
