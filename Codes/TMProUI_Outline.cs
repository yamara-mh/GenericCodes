using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TMPro
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [ExecuteAlways]
    public class TMProUI_Outline : MonoBehaviour
    {
        [SerializeField, Range(0f, 16f)] private float _width = 2f;
        [SerializeField] private List<Vector2> _directions = new();
#if UNITY_EDITOR
        [SerializeField] private OutlineMode _directionsPreset = OutlineMode.Dir4a;
        public enum OutlineMode
        {
            Custom = 0,
            Dir4a = 1,
            Dir4b = 2,
            Dir6 = 3,
            Dir8 = 5,
            Dir4b_Dir4a = 6,
            Dir6_Dir4b = 8,
            Dir8_Dir4b = 9,
        }
#endif

        private TextMeshProUGUI _text = null;
        private RectTransform _rendererRoot = null;
        [SerializeField] private List<CanvasRenderer> _renderers = new();

        private void OnEnable()
        {
            _text ??= transform.GetComponent<TextMeshProUGUI>();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(CheckUpdateText);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += UpdateRenderer;
                return;
            }
#endif
            UpdateRenderer();
        }
        private void Start()
        {
            _text.ObserveEveryValueChanged(t => t.enabled).Subscribe(enabled =>
            {
                if (enabled) OnEnable();
                else OnDisable();
            }).AddTo(this);
        }
        private void OnDisable()
        {
            var textEnabled = _text.enabled;
            _text.enabled = false;
            if (textEnabled) _text.enabled = true;

            CleanRenderers();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(CheckUpdateText);
        }
        private void CheckUpdateText(Object o)
        {
            if (o == _text) UpdateRenderer();
        }
        private void OnDestroy()
        {
            if (_rendererRoot != null)
            {
#if UNITY_EDITOR
                EditorApplication.delayCall += () => DestroyImmediate(_rendererRoot.gameObject);
#else
                Destroy(_rendererRect.gameObject);
#endif
            }
        }

        private void CleanRenderers()
        {
            foreach (var r in _renderers)
            {
                if (r != null) r.materialCount = 0;
            }
        }
        private void CreateRendererRoot()
        {
            _rendererRoot = new GameObject(nameof(TMProUI_Outline), typeof(RectTransform)).GetComponent<RectTransform>();
            _rendererRoot.SetParent(_text.transform, false);
        }
        public void UpdateRenderer()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorApplication.delayCall -= UpdateRenderer;
#endif
            if (_rendererRoot == null) CreateRendererRoot();
            CleanRenderers();
            if (!enabled || !_text.enabled || string.IsNullOrEmpty(_text.text)) return;

            var rCount = 0;
            var meshes = new List<Mesh>();
            var mats = new List<Material>();

            for (int i = Math.Min(_text.textInfo.materialCount, _text.textInfo.meshInfo.Length) - 1; i >= 0; i--)
            {
                var mesh = _text.textInfo.meshInfo[i].mesh;
                var mat = _text.textInfo.meshInfo[i].material;
                meshes.Add(mesh);
                mats.Add(mat);
                DrawOutlines(mesh, mat);
            }
            for (int i = meshes.Count - 1; i >= 0; i--) Draw(meshes[i], mats[i], Vector2.zero);

            _text.ClearMesh();


            void DrawOutlines(Mesh mesh, Material material)
            {
                foreach (var dir in _directions) Draw(mesh, material, dir * _width);
                meshes.Add(mesh);
                mats.Add(material);
            }
            void Draw(Mesh mesh, Material material, Vector2 shift)
            {
                var renderer = GetRenderer();
                var rect = renderer.transform as RectTransform;
                rect.localPosition = shift;
                renderer.materialCount = 1;
                renderer.SetMaterial(material, 0);
                renderer.SetMesh(mesh);
                rCount++;
            }
            CanvasRenderer GetRenderer()
            {
                if (_renderers.Count <= rCount) _renderers.Add(CreateRenderer());
                else if (_renderers[rCount] == null) _renderers[rCount] = CreateRenderer();
                return _renderers[rCount];
            }
            CanvasRenderer CreateRenderer()
            {
                var renderer = new GameObject(nameof(Renderer), typeof(RectTransform)).AddComponent<CanvasRenderer>();
                var rect = renderer.transform as RectTransform;
                rect.SetParent(_rendererRoot, false);
                rect.sizeDelta = Vector2.zero;
                return renderer;
            }
        }

        public void SetWidth(float width, bool update = true)
        {
            _width = width;
            if (update) UpdateRenderer();
        }
        public void SetDirections(List<Vector2> directions, bool update = true)
        {
            var prevDirectionsCount = directions.Count;
            if (prevDirectionsCount != directions.Count) RemoveAllRenderers();
            _directions = directions.ToList();
            if (update) UpdateRenderer();
        }
        private void RemoveAllRenderers()
        {
            for (int i = _renderers.Count - 1; i >= 0; i--)
            {
                var renderer = _renderers[i];
                if (renderer != null)
                {
#if UNITY_EDITOR
                    EditorApplication.delayCall += () =>
                    {
                        if (renderer != null) DestroyImmediate(renderer.gameObject);
                    };
#else
                    Destroy(_renderers[i].gameObject);
#endif
                }
                renderer.SetMesh(null);
                _renderers.RemoveAt(i);
            }
        }

#if UNITY_EDITOR

        private const float InSideWidthRate = 0.55f;

        private const float Sqr2Half = 0.7071068f;
        private const float Sqr3Half = 0.8660254f;
        private const float Sqr225s = 0.3826835f;
        private const float Sqr225l = 0.9238796f;
        private static Vector2[] Dir4a => new Vector2[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
        private static Vector2[] Dir4b => new Vector2[] { new(Sqr2Half, Sqr2Half), new(-Sqr2Half, Sqr2Half), new(-Sqr2Half, -Sqr2Half), new(Sqr2Half, -Sqr2Half) };
        private static Vector2[] Dir6 => new Vector2[] { Vector2.right, new(0.5f, Sqr3Half), new(-0.5f, Sqr3Half), Vector2.left, new(-0.5f, -Sqr3Half), new(0.5f, -Sqr3Half) };
        private static Vector2[] Dir8 => new Vector2[] { new(Sqr225l, Sqr225s), new(1f - Sqr225s, Sqr225l), new(-Sqr225s, Sqr225l), new(-Sqr225l, Sqr225s), new(-Sqr225l, -Sqr225s), new(-Sqr225s, -Sqr225l), new(1f - Sqr225s, -Sqr225l), new(Sqr225l, -Sqr225s) };

        private List<Vector2> _prevDirections = new();
        private bool _creatingRoot = false;

        private void OnValidate()
        {
            SetPreset(_directionsPreset);
            if (UpdatePrevDirections()) RemoveAllRenderers();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += UpdateRenderer;
                return;
            }
#endif
            UpdateRenderer();
        }

        private void SetPreset(OutlineMode mode)
        {
            switch (mode)
            {
                case OutlineMode.Dir4a:
                    _directions = Dir4a.ToList();
                    break;
                case OutlineMode.Dir4b:
                    _directions = Dir4b.ToList();
                    break;
                case OutlineMode.Dir6:
                    _directions = Dir6.ToList();
                    break;
                case OutlineMode.Dir8:
                    _directions = Dir8.ToList();
                    break;
                case OutlineMode.Dir4b_Dir4a:
                    _directions = Dir4b.ToList();
                    foreach (var d in Dir4a) _directions.Add(d * InSideWidthRate);
                    break;
                case OutlineMode.Dir6_Dir4b:
                    _directions = Dir6.ToList();
                    foreach (var d in Dir4b) _directions.Add(d * InSideWidthRate);
                    break;
                case OutlineMode.Dir8_Dir4b:
                    _directions = Dir8.ToList();
                    foreach (var d in Dir4b) _directions.Add(d * InSideWidthRate);
                    break;
                case OutlineMode.Custom:
                default: break;
            }
        }
        private bool UpdatePrevDirections()
        {
            if (_prevDirections.Count != _directions.Count)
            {
                _prevDirections = new(_directions);
                RemoveAllRenderers();
                return true;
            }
            for (int i = _directions.Count - 1; i >= 0; i--)
            {
                if (_prevDirections[i] == _directions[i]) continue;
                _prevDirections = new(_directions);
                RemoveAllRenderers();
                return true;
            }
            return false;
        }
#endif
    }
}
