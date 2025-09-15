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

        public static AudioSource SetTime(this AudioSource audioSource, float second)
        {
            audioSource.time = second;
            return audioSource;
        }
        public static AudioSource Skip(this AudioSource audioSource, float second)
        {
            audioSource.time += second;
            return audioSource;
        }

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

        public static AudioSource SetMixer(this AudioSource audioSource, AudioMixerGroup group)
        {
            audioSource.outputAudioMixerGroup = group;
            return audioSource;
        }
        public static AudioSource SetMixerFrom(this AudioSource audioSource, AudioSource target)
        {
            if (audioSource == null) return null;
            audioSource.outputAudioMixerGroup = target.outputAudioMixerGroup;
            return audioSource;
        }

        public static AudioSource SetSpatialBlend(this AudioSource audioSource, float spatialBlend)
        {
            audioSource.spatialBlend = spatialBlend;
            return audioSource;
        }

        public static AudioSource SetDopplerLevel(this AudioSource audioSource, float dopplerLevel)
        {
            audioSource.dopplerLevel = dopplerLevel;
            return audioSource;
        }

        public static AudioSource SetSpread(this AudioSource audioSource, float spread)
        {
            audioSource.spread = spread;
            return audioSource;
        }
        public static AudioSource SetPlayOnAwake(this AudioSource audioSource, bool flag)
        {
            audioSource.playOnAwake = flag;
            return audioSource;
        }
        public static AudioSource SetLoop(this AudioSource audioSource, bool flag)
        {
            audioSource.loop = flag;
            return audioSource;
        }

        public static AudioSource SetBypassSettings(this AudioSource audioSource, bool bypassEffects, bool bypassListenerEffects, bool bypassReverbZones)
        {
            audioSource.bypassEffects = bypassEffects;
            audioSource.bypassListenerEffects = bypassListenerEffects;
            audioSource.bypassReverbZones = bypassReverbZones;
            return audioSource;
        }


        /*// Infrequently used functions

        public static AudioSource SetBypassEffects(this AudioSource audioSource, bool flag)
        {
            audioSource.bypassEffects = flag;
            return audioSource;
        }
        public static AudioSource SetBypassListenerEffects(this AudioSource audioSource, bool flag)
        {
            audioSource.bypassListenerEffects = flag;
            return audioSource;
        }
        public static AudioSource SetBypassReverbZones(this AudioSource audioSource, bool flag)
        {
            audioSource.bypassReverbZones = flag;
            return audioSource;
        }
        public static AudioSource SetMute(this AudioSource audioSource, bool flag)
        {
            audioSource.mute = flag;
            return audioSource;
        }

        public static AudioSource SetTimeSamples(this AudioSource audioSource, int timeSamples)
        {
            audioSource.timeSamples = timeSamples;
            return audioSource;
        }
        public static AudioSource AddTimeSamples(this AudioSource audioSource, int timeSamples)
        {
            audioSource.timeSamples += timeSamples;
            return audioSource;
        }
        
        public static AudioSource SetStereoPan(this AudioSource audioSource, float panStereo)
        {
            audioSource.panStereo = panStereo;
            return audioSource;
        }
        public static AudioSource SetStereoPan(this AudioSource audioSource, float panStereo, float range)
        {
            audioSource.panStereo = panStereo + Random.Range(-range, range);
            return audioSource;
        }

        public static AudioSource SetIgnoreListenerPause(this AudioSource audioSource, bool flag)
        {
            audioSource.ignoreListenerPause = flag;
            return audioSource;
        }

        public static AudioSource SetSpatialize(this AudioSource audioSource, bool flag)
        {
            audioSource.spatialize = flag;
            return audioSource;
        }

        public static AudioSource SetSpatializePostEffects(this AudioSource audioSource, bool flag)
        {
            audioSource.spatializePostEffects = flag;
            return audioSource;
        }

        public static AudioSource SetVelocityUpdateMode(this AudioSource audioSource, AudioVelocityUpdateMode mode)
        {
            audioSource.velocityUpdateMode = mode;
            return audioSource;
        }
        
        public static AudioSource SetRolloffMode(this AudioSource audioSource, AudioRolloffMode mode)
        {
            audioSource.rolloffMode = mode;
            return audioSource;
        }
        // */
    }
}
