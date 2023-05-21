using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yamara.Audio
{
    [ExecuteAlways]
    [RequireComponent(typeof(AudioReverbZone))]
    public class SelfAudioReverbZone : MonoBehaviour
    {
        private static readonly float[] GenericParameters = new[]
        {
            0f,
            -1000f,
            -100f,
            0f,
            1.49f,
            0.83f,
            -2602f,
            0.007f,
            200f,
            0.011f,
            5000f,
            250f,
            100f,
            100f,
        };

        public static LinkedList<SelfAudioReverbZone> SortedInstances = new();

        [SerializeField] public AudioReverbZone Reverb;
        [SerializeField] public FilterMode Mode = FilterMode.Override;
        public enum FilterMode
        {
            Add = 0,
            Override = 1,
        }
        [SerializeField] private int _priority;
        [SerializeField, Range(-10000f, 0f)] public float dryLevel;

        [SerializeField] private bool UseCut = false;
        [SerializeField] public Vector3 CutMax;
        [SerializeField] public Vector3 CutMin;

        public Transform CachedTransform { get; private set; }

        public float MaxSqrDistance { get; private set; }
        public float MinSqrDistance { get; private set; }


        private void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            UpdateTransform();
            UpdateMinDistance(Reverb.minDistance);
            UpdateMaxDistance(Reverb.maxDistance);
            AddInstances();
        }
        private void AddInstances()
        {
            var node = SortedInstances.First;
            while (node != null)
            {
                if (_priority > node.Value._priority)
                {
                    node = node.Next;
                    continue;
                }
                SortedInstances.AddBefore(node, new LinkedListNode<SelfAudioReverbZone>(this));
                return;
            }
            SortedInstances.AddLast(this);
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            SortedInstances.Remove(this);
        }

        public IEnumerable<float> GetValues()
        {
            yield return dryLevel;
            yield return Reverb.room;
            yield return Reverb.roomHF;
            yield return Reverb.roomLF;
            yield return Reverb.decayTime;
            yield return Reverb.decayHFRatio;
            yield return Reverb.reflections;
            yield return Reverb.reflectionsDelay;
            yield return Reverb.reverb;
            yield return Reverb.reverbDelay;
            yield return Reverb.HFReference;
            yield return Reverb.LFReference;
            yield return Reverb.diffusion;
            yield return Reverb.density;
        }
        public IEnumerable<float> GetAddValues()
        {
            int index = 0;
            foreach (var v in GetValues()) yield return v - GenericParameters[index++];
        }

        public void UpdateMinDistance(float distance)
        {
            Reverb.minDistance = distance;
            MinSqrDistance = distance * distance;
        }
        public void UpdateMaxDistance(float distance)
        {
            Reverb.maxDistance = distance;
            MaxSqrDistance = distance * distance;
        }
        public void SetMinSqrDistance(float sqrDistance)
        {
            Reverb.minDistance = Mathf.Sqrt(sqrDistance);
            MinSqrDistance = sqrDistance;
        }
        public void SetMaxSqrDistance(float sqrDistance)
        {
            Reverb.maxDistance = Mathf.Sqrt(sqrDistance);
            MaxSqrDistance = sqrDistance;
        }
        public void UpdateTransform(Transform transform) => CachedTransform = transform;
        public void UpdateTransform() => CachedTransform = transform;

        public void UpdatePriority(int priority)
        {
            _priority = priority;
            SortedInstances.Remove(this);
            AddInstances();
        }

        public bool IsInSide(Vector3 pos)
        {
            return !UseCut || (
                pos.x >= CutMin.x && pos.x <= CutMax.x
                && pos.y >= CutMin.y && pos.y <= CutMax.y
                && pos.z >= CutMin.z && pos.z <= CutMax.z);
        }

#if UNITY_EDITOR

        [SerializeField] Color _CutDrawColor = Color.magenta;

        private void OnValidate()
        {
            CachedTransform ??= transform;

            Reverb ??= GetComponent<AudioReverbZone>();
            UpdateMinDistance(Reverb.minDistance);
            UpdateMaxDistance(Reverb.maxDistance);

            if (Application.isPlaying) return;
            if (UseCut == false)
            {
                CutMin = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                CutMax = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            }
            else
            {
                if (CutMin.x > 0f) CutMin.x = -CutMin.x;
                if (CutMin.y > 0f) CutMin.y = -CutMin.y;
                if (CutMin.z > 0f) CutMin.z = -CutMin.z;

                if (CutMax.x < 0f) CutMin.x = -CutMin.x;
                if (CutMax.y < 0f) CutMin.y = -CutMin.y;
                if (CutMax.z < 0f) CutMin.z = -CutMin.z;
            }
        }

        private void Update()
        {
            if (!UseCut) return;
            Debug.DrawRay(transform.position, transform.right * CutMax.x, _CutDrawColor);
            Debug.DrawRay(transform.position, -transform.right * CutMin.x, _CutDrawColor);
            Debug.DrawRay(transform.position, transform.up * CutMax.y, _CutDrawColor);
            Debug.DrawRay(transform.position, -transform.up * CutMin.y, _CutDrawColor);
            Debug.DrawRay(transform.position, transform.forward * CutMax.z, _CutDrawColor);
            Debug.DrawRay(transform.position, -transform.forward * CutMin.z, _CutDrawColor);
        }
#endif
    }
}
