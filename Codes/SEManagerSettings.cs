using UnityEngine.Audio;
using UnityEngine;

namespace Yamara
{
    [CreateAssetMenu(fileName = nameof(SEManagerSettings), menuName = nameof(ScriptableObject) + "/Create " + nameof(SEManagerSettings))]
    public class SEManagerSettings : ScriptableObject
    {
        [SerializeField] public AudioSource OneShotAudioSourcePrefab = null;

        [SerializeField] public AudioSource AudioSourcePrefab = null;
        [SerializeField] public int MaxAudioSource = 8;

        [SerializeField, Range(0f, 1f)] public float DefaultVolume = 1f;

        [SerializeField] public float DefaultMaxDistance = 50f;

        [SerializeField, Range(0f, 360f)] public float DefaultSpread = 30f;

        [SerializeField, Range(0f, 5f)] public float DefaultDopplerLevel = 1f;
    }
}
