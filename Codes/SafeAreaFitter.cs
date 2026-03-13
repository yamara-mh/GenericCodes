using R3;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// RectTransform を Screen.safeArea へ自動的に合わせるコンポーネント
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeAreaFitter : UIBehaviour
{
    public enum ScreenMatchMode
    {
        /// <summary>safeArea の内側へ収まるように localScale と position を調整。比率維持</summary>
        Expand,
        /// <summary>safeArea を満たすように localScale と position を調整。比率維持</summary>
        Shrink,
        /// <summary>safeArea の横幅に合わせて localScale と position を調整。比率維持</summary>
        MatchWidth,
        /// <summary>safeArea の縦幅に合わせて localScale と position を調整。比率維持</summary>
        MatchHeight,
        /// <summary>safeArea とピッタリ一致するように RectTransform.sizeDelta を調整</summary>
        Stretch,
    }

    [SerializeField] private ScreenMatchMode matchMode = ScreenMatchMode.Expand;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Canvas canvas;

    protected override void Awake()
    {
        ApplySafeArea(Screen.safeArea);

        if (Application.isPlaying == false) return;

        Observable.EveryUpdate()
            .Select(_ => (Screen.safeArea, Screen.width, Screen.height))
            .DistinctUntilChanged()
            .ThrottleFirst(TimeSpan.FromSeconds(0.1f))
            .Subscribe(args => ApplySafeArea(args.safeArea))
            .AddTo(this);
    }

#if UNITY_EDITOR
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2Int lastScreenSize = Vector2Int.zero;

    private void Update()
    {
        if (Application.isPlaying) return;

        if (lastSafeArea == Screen.safeArea &&
            lastScreenSize.x == Screen.width &&
            lastScreenSize.y == Screen.height) return;

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        ApplySafeArea(Screen.safeArea);
    }
    protected override void OnValidate()
    {
        base.OnValidate();

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ApplySafeArea(Screen.safeArea);
        };
    }
#endif

    public void ApplySafeArea(Rect safeArea)
    {
        if (rectTransform == null) return;
        if (canvas == null) return;

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return; // Canvas直下ではない、または親がない場合は無効

        // Overlayモードの場合はCameraをnullにする必要がある
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // スクリーン座標系のSafeAreaを、親のRectTransformのローカル座標系における矩形（Rect）に変換する
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, safeArea.min, cam, out Vector2 minLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, safeArea.max, cam, out Vector2 maxLocal);
        Rect targetLocalRect = new Rect(minLocal, maxLocal - minLocal);

        if (matchMode == ScreenMatchMode.Stretch)
        {
            // --- Stretch モード ---
            // scaleはリセットし、sizeDeltaとlocalPositionのみでターゲット矩形にピッタリ合わせる
            rectTransform.localScale = Vector3.one;

            // Anchorは維持したまま、ターゲットの幅と高さに合わせる
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetLocalRect.width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetLocalRect.height);

            // Pivotのズレを考慮して中心位置を合わせる
            Vector2 targetLocalPos = targetLocalRect.center - rectTransform.rect.center;
            rectTransform.localPosition = new Vector3(targetLocalPos.x, targetLocalPos.y, rectTransform.localPosition.z);
        }
        else
        {
            // --- Expand, Shrink, MatchWidth, MatchHeight モード ---
            float currentWidth = rectTransform.rect.width;
            float currentHeight = rectTransform.rect.height;

            if (currentWidth <= 0 || currentHeight <= 0) return; // ゼロ除算防止

            // ターゲット領域に対するスケール比率を計算
            float scaleX = targetLocalRect.width / currentWidth;
            float scaleY = targetLocalRect.height / currentHeight;
            float finalScale = 1f;

            switch (matchMode)
            {
                case ScreenMatchMode.Expand: finalScale = Mathf.Min(scaleX, scaleY); break;
                case ScreenMatchMode.Shrink: finalScale = Mathf.Max(scaleX, scaleY); break;
                case ScreenMatchMode.MatchWidth: finalScale = scaleX; break;
                case ScreenMatchMode.MatchHeight: finalScale = scaleY; break;
            }

            // sizeDelta と anchor は一切変更せず、localScale を適用
            rectTransform.localScale = new Vector3(finalScale, finalScale, rectTransform.localScale.z);

            // スケール適用後のPivotのズレを考慮して、SafeAreaの中心と自身の中心を完全に一致させる
            Vector2 targetLocalPos = targetLocalRect.center - rectTransform.rect.center * finalScale;
            rectTransform.localPosition = new Vector3(targetLocalPos.x, targetLocalPos.y, rectTransform.localPosition.z);
        }
    }
}
