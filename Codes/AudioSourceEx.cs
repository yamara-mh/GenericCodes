using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Extensions
{
    public static class AudioSourceEx
    {
        public static AudioSource Play(this AudioSource audioSource, float delay)
        {
            audioSource.PlayDelayed(delay);
            return audioSource;
        }
        public static AudioSource Play(this AudioSource audioSource, AudioClip clip, float delay = 0f)
            => audioSource.SetClip(clip).Play(delay);
        public static AudioSource Play(this AudioSource audioSource, AudioClip[] clips, float delay = 0f)
            => audioSource.SetClip(clips).Play(delay);
        public static AudioSource Play(this AudioSource audioSource, AudioClip clip, float pitch, float pitchRange, float delay = 0f)
            => audioSource.SetClip(clip).SetPitch(pitch, pitchRange).Play(delay);
        public static AudioSource Play(this AudioSource audioSource, AudioClip[] clips, float pitch, float pitchRange, float delay = 0f)
            => audioSource.SetClip(clips).SetPitch(pitch, pitchRange).Play(delay);
        public static AudioSource Play(this AudioSource audioSource, float volume, float volumeRange, float pitch, float pitchRange, float delay = 0f)
            => audioSource.SetVolume(volume, volumeRange).SetPitch(pitch, pitchRange).Play(delay);
        public static AudioSource Play(this AudioSource audioSource, AudioClip clip, float volume, float volumeRange, float pitch, float pitchRange, float delay = 0f)
            => audioSource.SetClip(clip).SetVolume(volume, volumeRange).SetPitch(pitch, pitchRange).Play(delay);
        public static AudioSource Play(this AudioSource audioSource, AudioClip[] clips, float volume, float volumeRange, float pitch, float pitchRange, float delay = 0f)
            => audioSource.SetClip(clips).SetVolume(volume, volumeRange).SetPitch(pitch, pitchRange).Play(delay);

        public static AudioSource SetClip(this AudioSource audioSource, AudioClip clip)
        {
            audioSource.clip = clip;
            return audioSource;
        }
        public static AudioSource SetClip(this AudioSource audioSource, params AudioClip[] clips)
        {
            audioSource.clip = clips[Random.Range(0, clips.Length)];
            return audioSource;
        }

        public static AudioSource SetVolume(this AudioSource audioSource, float volume)
        {
            audioSource.volume = volume;
            return audioSource;
        }
        public static AudioSource SetVolume(this AudioSource audioSource, float volume, float range)
        {
            audioSource.volume = volume + Random.Range(-range, range);
            return audioSource;
        }

        public static AudioSource SetPitch(this AudioSource audioSource, float pitch)
        {
            audioSource.pitch = pitch;
            return audioSource;
        }
        public static AudioSource SetPitch(this AudioSource audioSource, float pitch, float range)
        {
            audioSource.pitch = pitch + Random.Range(-range, range);
            return audioSource;
        }

        public static AudioSource SetPriority(this AudioSource audioSource, int priority)
        {
            audioSource.priority = priority;
            return audioSource;
        }

        public static AudioSource SetDistance(this AudioSource audioSource, float max, float min = 1f)
        {
            audioSource.minDistance = min;
            audioSource.maxDistance = max;
            return audioSource;
        }

        public static AudioSource SetSpatialBlend(this AudioSource audioSource, float spatialBlend)
        {
            audioSource.spatialBlend = spatialBlend;
            return audioSource;
        }

        public static AudioSource SetSpread(this AudioSource audioSource, float spread)
        {
            audioSource.spread = spread;
            return audioSource;
        }

        public static AudioSource SetDopplerLevel(this AudioSource audioSource, float dopplerLevel)
        {
            audioSource.dopplerLevel = dopplerLevel;
            return audioSource;
        }
    }
}
