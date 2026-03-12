using UnityEngine;
using R3;
using System;

/// <summary>
/// SafeArea に収まるようにScaleとPositionを調整する
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaExpandScaler : MonoBehaviour
{
    private RectTransform _rectTransform;
    private RectTransform _parentRectTransform;
    private Canvas _canvas;

    private void Start()
    {
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (_parentRectTransform == null) _parentRectTransform = transform.parent as RectTransform;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

        ApplySafeArea();

        Observable.EveryUpdate()
            .Select(_ => new Vector2Int(Screen.width, Screen.height))
            .DistinctUntilChanged()
            .ThrottleFirst(TimeSpan.FromSeconds(0.1f))
            .Subscribe(_ => ApplySafeArea())
            .AddTo(this);
    }

    /// <summary>
    /// SafeAreaに合わせてScaleとPositionを調整
    /// </summary>
    public void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // UI用カメラの取得（Overlayの場合はnullが必要）
        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        // スクリーン座標のSafeAreaの四隅を、親のローカル座標に変換する
        Vector2 screenBottomLeft = new Vector2(safeArea.xMin, safeArea.yMin);
        Vector2 screenTopRight = new Vector2(safeArea.xMax, safeArea.yMax);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRectTransform, screenBottomLeft, uiCamera, out Vector2 localBottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRectTransform, screenTopRight, uiCamera, out Vector2 localTopRight);

        // 親空間におけるSafeAreaのターゲット幅と高さを算出
        float targetWidth = localTopRight.x - localBottomLeft.x;
        float targetHeight = localTopRight.y - localBottomLeft.y;

        // 現在のRectTransformの元のサイズ（sizeDeltaではなく実際のrect.sizeを使用）
        float currentWidth = _rectTransform.rect.width;
        float currentHeight = _rectTransform.rect.height;

        if (currentWidth == 0 || currentHeight == 0) return;

        // 必要なスケールを計算
        float scaleX = targetWidth / currentWidth;
        float scaleY = targetHeight / currentHeight;

        // アスペクト比維持
        float minScale = Mathf.Min(scaleX, scaleY);
        scaleX = minScale;
        scaleY = minScale;

        _rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);

        // ピボット（Pivot）を考慮してローカルポジションを計算
        Vector2 pivot = _rectTransform.pivot;
        Vector2 targetCenter = (localBottomLeft + localTopRight) / 2f;

        float scaledWidth = currentWidth * scaleX;
        float scaledHeight = currentHeight * scaleY;

        // ターゲットの中心座標から、Pivotのズレ分を補正した位置が正しいlocalPosition
        float posX = targetCenter.x + (pivot.x - 0.5f) * scaledWidth;
        float posY = targetCenter.y + (pivot.y - 0.5f) * scaledHeight;

        _rectTransform.localPosition = new Vector3(posX, posY, _rectTransform.localPosition.z);
    }
}
