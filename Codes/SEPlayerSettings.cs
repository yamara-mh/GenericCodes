using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [CreateAssetMenu(fileName = nameof(SEPlayerSettings), menuName = nameof(ScriptableObject) + "/Create " + nameof(SEPlayerSettings))]
    public class SEPlayerSettings : ScriptableObject
    {
        [SerializeField] public int MaxAudioSource = 8;

        [SerializeField] public AudioSource InitialAudioSourcePrefab = null;
        [SerializeField] public AudioMixerGroup InitialAudioMixerGroup;

        [SerializeField, Range(0f, 1f)] public float DefaultVolume = 1f;
        [SerializeField, Range(0f, 1f)] public float DefaultPitch = 1f;
        [SerializeField] public float DefaultMaxDistance = 50f;
    }
}
