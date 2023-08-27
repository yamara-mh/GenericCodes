using UnityEngine;

namespace Audio
{
    public class BorderAudioSourceController : MonoBehaviour
    {
        public enum BorderEnum
        {
            Shpere = 0,
            Cylinder = 1,
        }

        [SerializeField] public float Radius = 0.5f;
        [SerializeField] private Transform centerTransfrom;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool findAndAttachAudioListenerOnStart = true;
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private BorderEnum borderType = BorderEnum.Cylinder;
        [SerializeField] private bool alighYaxisToListener = true;
        [SerializeField] private bool loccyScaleXToHalfRadius = true;

        public AudioListener Listener { get; private set; }

        private Transform listenerTransform;
        private Transform audioSourceTransform;

        private void Start()
        {
            audioSourceTransform = audioSource.transform;
            if (findAndAttachAudioListenerOnStart) FindAndAttachAudioListener();
        }

        private void LateUpdate()
        {
            if (autoUpdate) UpdateAudioSource();
        }

        public bool AttachAudioListenerByTag(string tag)
        {
            if (GameObject.FindGameObjectWithTag(tag).TryGetComponent<AudioListener>(out var listener))
            {
                SetListener(listener);
                return true;
            }
            return false;
        }
        public bool FindAndAttachAudioListener()
        {
            foreach (GameObject obj in FindObjectsOfType(typeof(GameObject)))
            {
                if (obj.activeInHierarchy && obj.TryGetComponent<AudioListener>(out var listener))
                {
                    SetListener(listener);
                    return true;
                }
            }
            return false;
        }
        public void SetListener(AudioListener listener)
        {
            Listener = listener;
            listenerTransform = Listener.transform;
        }

        public void UpdateAudioSource(float? radius = null)
        {
            if (radius != null) Radius = radius.Value;
            UpdateAudioSource();
        }
        public void UpdateAudioSource()
        {
            var vector = listenerTransform.position - centerTransfrom.position;
            if (borderType == BorderEnum.Cylinder) vector.y = 0f;

            Vector3 direction;
            if (vector.sqrMagnitude == 0f) direction = Vector3.zero;
            else direction = vector.normalized;

            if (loccyScaleXToHalfRadius) Radius = transform.lossyScale.x / 2f;
            var audioPos = centerTransfrom.position + direction * Radius;
            if (alighYaxisToListener) audioPos.y = listenerTransform.position.y;
            audioSourceTransform.position = audioPos;
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            centerTransfrom ??= transform;
            audioSource ??= GetComponentInChildren<AudioSource>();
        }
#endif
    }
}
