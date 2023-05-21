using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yamara.Audio
{
    public class SelfAudioReverbListener : MonoBehaviour
    {
        private const int ParametorsCount = 14;

        [SerializeField, Range(1, 15)] public int UpdateIntervalFrame = 2;
        [SerializeField] public Transform SensorTransform;
        [SerializeField] public AudioReverbFilter ListenerFilter;
        [SerializeField] public AudioMixer AudioMixer;
        [SerializeField] private string _audioMixerParameterFormat = "se_{0}";

        private float[] _defaultValues = new float[ParametorsCount];
        private float[] _values = new float[ParametorsCount];
        private string[] _audioMixerParameterNames;
        private int _intervalCount;

        private LinkedList<(SelfAudioReverbZone zone, float sqrDis)> list = new();

        private void Awake()
        {
            if (AudioMixer != null) InitParameterNames();
            InitDefaultValues();
            UpdateParameters();
        }

        private void Update()
        {
            if (--_intervalCount > 0) return;
            _intervalCount = UpdateIntervalFrame;
            UpdateParameters();
        }
        public void UpdateParameters()
        {
            CleanValues();
            CalcValues();
            UpdateValues();
        }

        private void CalcValues()
        {
            var pos = SensorTransform.position;

            foreach (var zone in SelfAudioReverbZone.SortedInstances)
            {
                if (!zone.enabled) continue;
                var sqrDis = (pos - zone.CachedTransform.position).sqrMagnitude;
                if (sqrDis >= zone.MaxSqrDistance) continue;
                if (!zone.IsInSide(zone.CachedTransform.InverseTransformPoint(pos))) continue;

                if (zone.Mode == SelfAudioReverbZone.FilterMode.Override && sqrDis <= zone.MinSqrDistance)
                {
                    SetValues(zone);
                    break;
                }
                else list.AddFirst((zone, sqrDis));
            }
            foreach (var data in list)
            {
                var distanceRate = 1f;
                if (data.sqrDis > data.zone.MinSqrDistance)
                {
                    var diff = Mathf.Sqrt(data.sqrDis) - data.zone.Reverb.minDistance;
                    var delta = data.zone.Reverb.maxDistance - data.zone.Reverb.minDistance;
                    distanceRate = 1f - diff / delta;
                }
                if (data.zone.Mode == SelfAudioReverbZone.FilterMode.Add) AddValues(data.zone, distanceRate);
                else FadeValues(data.zone, distanceRate);
            }
            list.Clear();
        }

        private void CleanValues()
        {
            int index = 0;
            foreach (var value in _defaultValues) _values[index++] = value;
        }
        private void SetValues(SelfAudioReverbZone zone)
        {
            int index = 0;
            foreach (var value in zone.GetValues()) _values[index++] = value;
        }
        private void FadeValues(SelfAudioReverbZone zone, float rate)
        {
            int index = 0;
            foreach (var value in zone.GetValues())
            {
                _values[index] = Mathf.Lerp(_values[index++], value, rate);
            }
        }
        private void AddValues(SelfAudioReverbZone zone, float rate)
        {
            int index = 0;
            foreach (var value in zone.GetAddValues()) _values[index] += value * rate;
        }

        public void UpdateValues()
        {
            if (AudioMixer != null)
            {
                for (int i = ParametorsCount - 1; i >= 0; i--) AudioMixer.SetFloat(_audioMixerParameterNames[i], _values[i]);
            }
            if (ListenerFilter != null)
            {
                ListenerFilter.dryLevel = _values[0];
                ListenerFilter.room = _values[1];
                ListenerFilter.roomHF = _values[2];
                ListenerFilter.roomLF = _values[3];
                ListenerFilter.decayTime = _values[4];
                ListenerFilter.decayHFRatio = _values[5];
                ListenerFilter.reflectionsLevel = _values[6];
                ListenerFilter.reflectionsDelay = _values[7];
                ListenerFilter.reverbLevel = _values[8];
                ListenerFilter.reverbDelay = _values[9];
                ListenerFilter.hfReference = _values[10];
                ListenerFilter.lfReference = _values[11];
                ListenerFilter.diffusion = _values[12];
                ListenerFilter.density = _values[13];
            }
        }
        public void InitParameterNames()
        {
            _audioMixerParameterNames = new string[ParametorsCount];
            AudioReverbFilter filter;

            var filterNames = new string[]
            {
                nameof(filter.dryLevel),
                nameof(filter.room),
                nameof(filter.roomHF),
                nameof(filter.roomLF),
                nameof(filter.decayTime),
                nameof(filter.decayHFRatio),
                nameof(filter.reflectionsLevel),
                nameof(filter.reflectionsDelay),
                nameof(filter.reverbLevel),
                nameof(filter.reverbDelay),
                nameof(filter.hfReference),
                nameof(filter.lfReference),
                nameof(filter.diffusion),
                nameof(filter.density),
            };
            var index = 0;
            foreach (var n in filterNames) _audioMixerParameterNames[index++] = string.Format(_audioMixerParameterFormat, n);
        }
        public void InitDefaultValues()
        {
            if (AudioMixer != null)
            {
                var defaultValues = new List<float>();
                foreach (var name in _audioMixerParameterNames)
                {
                    AudioMixer.GetFloat(name, out var value);
                    defaultValues.Add(value);
                }
                _defaultValues = defaultValues.ToArray();
            }
            if (ListenerFilter != null)
            {
                ListenerFilter.reverbPreset = AudioReverbPreset.User;
                var index = 0;
                foreach (var value in GetFilterValues(ListenerFilter)) _defaultValues[index++] = value;
            }
        }

        private IEnumerable<float> GetFilterValues(AudioReverbFilter filter)
        {
            yield return filter.dryLevel;
            yield return filter.room;
            yield return filter.roomHF;
            yield return filter.roomLF;
            yield return filter.decayTime;
            yield return filter.decayHFRatio;
            yield return filter.reflectionsLevel;
            yield return filter.reflectionsDelay;
            yield return filter.reverbLevel;
            yield return filter.reverbDelay;
            yield return filter.hfReference;
            yield return filter.lfReference;
            yield return filter.diffusion;
            yield return filter.density;
        }
    }
}
