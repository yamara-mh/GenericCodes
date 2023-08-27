using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Audio
{
    public class BorderAudioSourceController : MonoBehaviour
    {
        public enum BorderEnum
        {
            Sphere = 0,
            Cylinder = 1,
        }

        [Header("Border GameObject")]
        [SerializeField] private Transform centerTransfrom;
        [SerializeField] private BorderEnum borderType = BorderEnum.Cylinder;
        [SerializeField, Tooltip("Radius when Scale is 1")]
        private float defaultRadius = 0.5f;

        [Header("AudioSource")]
        [SerializeField] private AudioSource childAudioSource;
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private bool alignYPosToListener = true;

        [Header("AudioListener")]
        [SerializeField] private Transform listenerTransform;
        [SerializeField, Tooltip("Find an AudioListener and attach it to listenerTransfrom on Start()")]
        private bool findListenerOnStart = true;

        private Transform audioSourceTransform;

        private void Start()
        {
            audioSourceTransform = childAudioSource.transform;
            if (findListenerOnStart) SetListenerTransform(FindAudioListenerInScene().transform);
        }

        private void LateUpdate()
        {
            if (autoUpdate) UpdateAudioSource();
        }

        /// <summary>
        /// Find the AudioListener in the scene.
        /// If there are multiple candidates, activeInHierarchy will give priority to valid candidates.
        /// </summary>
        public AudioListener FindAudioListenerInScene()
        {
            var listeners = new List<AudioListener>();
            foreach (GameObject o in FindObjectsOfType(typeof(GameObject)))
            {
                if (o.TryGetComponent<AudioListener>(out var l)) listeners.Add(l);
            }
            if (listeners.Count == 0)
            {
                Debug.LogError("AudioListener not found");
                return null;
            }
            return listeners.OrderBy(l => l.gameObject.activeInHierarchy).FirstOrDefault();
        }
        public void SetListenerTransform(Transform listenerTransform) => this.listenerTransform = listenerTransform;
        public void SetDefaultRadius(float radius) => this.defaultRadius = radius;

        public void UpdateAudioSource()
        {
            var vector = listenerTransform.position - centerTransfrom.position;
            if (borderType == BorderEnum.Cylinder) vector.y = 0f;

            Vector3 direction;
            if (vector.sqrMagnitude == 0f) direction = Vector3.zero;
            else direction = vector.normalized;

            var audioPos = centerTransfrom.position + direction * defaultRadius * transform.lossyScale.x;
            if (alignYPosToListener) audioPos.y = listenerTransform.position.y;
            audioSourceTransform.position = audioPos;
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            centerTransfrom ??= transform;
            childAudioSource ??= GetComponentInChildren<AudioSource>();

            if (defaultRadius == 0f && centerTransfrom.TryGetComponent(out Renderer r))
            {
                defaultRadius = r.bounds.size.x / 2f / centerTransfrom.localScale.x;
            }
        }
#endif
    }
}
