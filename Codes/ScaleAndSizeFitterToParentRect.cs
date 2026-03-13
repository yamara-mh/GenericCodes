using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 親の範囲に丁度収まるように minSize から maxSize の間で sizeDelta を自動調整するコンポーネント
/// 親の範囲が minSize より狭いときは scale の縮小で対応し、レイアウト崩れを防ぐ
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class ScaleAndSizeFitterToParentRect : UIBehaviour
{
    [SerializeField] Vector2 minSize;
    [SerializeField] Vector2 maxSize;

    protected override void Start()
    {
        base.Start();
        Fit();
    }
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        Fit();
    }

#if UNITY_EDITOR
    protected virtual void Update()
    {
        if (Application.isPlaying) return;
        Fit();
    }
    protected override void OnValidate()
    {
        base.OnValidate();
        
        minSize = new Vector2(
            Mathf.Max(minSize.x, 1),
            Mathf.Max(minSize.y, 1));
        maxSize = new Vector2(
            Mathf.Max(minSize.x, maxSize.x),
            Mathf.Max(minSize.y, maxSize.y));
    }
#endif

    public void Fit()
    {
        var selfRect = transform as RectTransform;
        var parentRect = selfRect.parent as RectTransform;
        if (parentRect == null) return;

        var parentSize = parentRect.rect.size;

        // 1. 親の範囲内で minSize が丁度収まるように rectTransform.localScale を調整
        var fitScale = Mathf.Min(parentSize.x / minSize.x, parentSize.y / minSize.y);
        selfRect.localScale = new Vector3(fitScale, fitScale, 1f);

        // 2. 親の範囲内で maxSize が収まるように rectTransform.sizeDelta を調整
        selfRect.sizeDelta = new Vector2(
            Mathf.Min(parentSize.x / fitScale, maxSize.x),
            Mathf.Min(parentSize.y / fitScale, maxSize.y)
        );
    }
}
