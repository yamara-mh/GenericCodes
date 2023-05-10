using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks.Triggers;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TMPro
{
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas), typeof(TextMeshProUGUI))]
    public class TMProUI_Outline : MonoBehaviour
    {
        private const AdditionalCanvasShaderChannels CanvasShaderChannels
            = AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        [SerializeField] private TextMeshProUGUI _tmpText;
        [SerializeField, Range(0f, 16f)] public float Width = 2f;
        [SerializeField] public Vector2[] Directions = new Vector2[0];
        [SerializeField] private OutlineMode _preset = OutlineMode.Direction4;
        public enum OutlineMode
        {
            Custom = 0,
            Direction4 = 1,
            Direction6 = 2,
            Direction8 = 3,
        }

        [SerializeField] private Canvas _textCanvas;
        [SerializeField] private Canvas _outlineCanvas;

        private int _useRenderCount = 0;
        private List<CanvasRenderer> _renderers = new();

        private void Awake()
        {
            _tmpText ??= transform.GetComponent<TextMeshProUGUI>();
            SetTriggers();
        }
        private void SetTriggers()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            _tmpText.GetAsyncEnableTrigger().Subscribe(_ => _outlineCanvas.gameObject.SetActive(true)).AddTo(ct);
            _tmpText.GetAsyncDisableTrigger().Subscribe(_ => _outlineCanvas.gameObject.SetActive(false)).AddTo(ct);
            _tmpText.GetAsyncPostRenderTrigger().Subscribe(_ => UpdateRenderer()).AddTo(ct);
        }
        private void OnEnable()
        {
            _outlineCanvas?.gameObject.SetActive(true);
            UpdateRenderer();
        }
        private void OnDisable()
        {
            _outlineCanvas?.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_outlineCanvas == null) return;
#if UNITY_EDITOR
            EditorApplication.delayCall += () => DestroyImmediate(_outlineCanvas.gameObject);
#else
            Destroy(_outlineCanvas.gameObject);
#endif
        }

        private void UpdateCanvases()
        {
            if (_textCanvas == null)
            {
                _textCanvas = GetComponent<Canvas>();
                _textCanvas.overrideSorting = true;
                _textCanvas.additionalShaderChannels = CanvasShaderChannels;
            }
            if (_outlineCanvas == null)
            {
                _outlineCanvas = new GameObject(nameof(TMProUI_Outline), typeof(RectTransform)).AddComponent<Canvas>();
                _outlineCanvas.overrideSorting = true;
                _outlineCanvas.additionalShaderChannels = CanvasShaderChannels;

                var rect = (_outlineCanvas.transform as RectTransform);
                rect.parent = _tmpText.transform;
                rect.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                rect.sizeDelta = Vector2.zero;
            }
            if (_tmpText.canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                (_outlineCanvas.transform as RectTransform).localScale
                    = Vector3.one * _tmpText.canvas.transform.localScale.z;
            }
            _outlineCanvas.sortingOrder = _textCanvas.sortingOrder - 1;
        }
        public void UpdateRenderer()
        {
            UpdateCanvases();
            CleanRenderers(_useRenderCount);
            _useRenderCount = 0;
            var TmpTransform = _tmpText.rectTransform;

            DrawRender(_tmpText.mesh, _tmpText.fontMaterial);

            for (int ci = TmpTransform.childCount - 1; ci >= 0; ci--)
            {
                if (TmpTransform.GetChild(ci).TryGetComponent(out TMP_SubMeshUI tmp))
                {
                    DrawRender(tmp.mesh, tmp.material);
                }
            }

            void DrawRender(Mesh mesh, Material material)
            {
                var width = Width;
                if (_tmpText.canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    width /= _tmpText.canvas.transform.localScale.z;
                }
                foreach (var direction in Directions)
                {
                    if (_renderers.Count <= _useRenderCount) _renderers.Add(CreateCanvasRenderer());
                    var renderer = _renderers[_useRenderCount];
                    (renderer.transform as RectTransform).localPosition = direction * width;
                    renderer.materialCount = 1;
                    renderer.SetMaterial(material, 0);
                    renderer.SetMesh(mesh);
                    _useRenderCount++;
                }
            }
            CleanRenderers(_useRenderCount);
        }
        private CanvasRenderer CreateCanvasRenderer()
        {
            CanvasRenderer outline;
            outline = new GameObject(nameof(outline), typeof(RectTransform)).AddComponent<CanvasRenderer>();
            outline.transform.parent = _outlineCanvas.transform;
            var rect = (outline.transform as RectTransform);
            rect.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            rect.localScale = Vector3.one;
            rect.sizeDelta = Vector2.zero;
            return outline;
        }
        private void CleanRenderers(int useCount)
        {
            for (int i = _renderers.Count - 1; i >= useCount; i--)
            {
                var removedRenderer = _renderers[i];
                _renderers.RemoveAt(i);
                if (removedRenderer == null) continue;

#if UNITY_EDITOR
                EditorApplication.delayCall += () => DestroyImmediate(removedRenderer.gameObject);
#else
                Destroy(removedRenderer.gameObject);
#endif
            }

            for (int i = _renderers.Count - 1; i >= 0; i--)
            {
                if (_renderers[i] == null) _renderers[i] = CreateCanvasRenderer();
            }
        }
        public void SetPreset(OutlineMode mode)
        {
            switch (mode)
            {
                case OutlineMode.Direction4:
                    Directions = new Vector2[]
                    { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
                    break;
                case OutlineMode.Direction6:
                    Directions = new Vector2[]
                    { Vector2.right, new(0.5f, Mathf.Sqrt(3f) / 2f), new(-0.5f, Mathf.Sqrt(3f) / 2f),
                      Vector2.left, new(-0.5f, -Mathf.Sqrt(3f) / 2f), new(0.5f, -Mathf.Sqrt(3f) / 2f) };
                    break;
                case OutlineMode.Direction8:
                    Directions = new Vector2[]
                    { Vector2.right, new(1f, 1f), Vector2.up, new(-1f, 1f),
                      Vector2.left, new(-1f, -1f), Vector2.down, new(1f, -1f) };
                    break;
                case OutlineMode.Custom:
                default: break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            SetPreset(_preset);
            UpdateRenderer();
        }
#endif
    }
}
