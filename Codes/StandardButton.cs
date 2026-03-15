using R3;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通常のボタンを実装するコンポーネント
/// 押下中はサイズが変わり、クリックすると音が鳴る
/// </summary>
public class StandardButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float ZoomRate = 0.95f; // 押下中にサイズを変える
    private const float DefaultRaycastPaddingMargin = -15f; // 押下判定を少し広げる

    // コンポーネント追加時、ボタンの名前に含まれる文字列に応じてSEを設定
    private readonly string[] SelectSeKeywords = new string[] { "ok", "next", "confirm", "start", "play" };
    private readonly string[] CancelSeKeywords = new string[] { "close", "back", "cancel" };
    private readonly string[] ToggleSeKeywords = new string[] { "tab", "toggle", "switch" };

    [SerializeField] public bool interactable = true;
    [SerializeField] public Image image;
    [SerializeField] public SeEnum se;
    [SerializeField] public AudioClip overrideSeOrNull;
    [SerializeField] private RectTransform zoomTarget;
    [SerializeField] private bool zoomFlag = true;

    /// <summary>
    /// 押下時の拡縮で raycastTarget も拡縮してしまうため、padding を調整して大きさを一定にするフラグ
    /// zoomTarget 自身やその子や孫に対象の image が含まれていれば、自動的に有効になる
    /// </summary>
    [Header("Editor が自動で設定します")]
    [SerializeField] private bool needAdjustmentPaddingOnZoom = true;

    private Vector3 _scale;
    private Vector4 _padding;
    /// <summary>同時押し対策</summary>
    private int? activePointerId = null;

    private Subject<PointerEventData> onClick = new();
    public Observable<PointerEventData> OnClick => onClick;

    private void Awake()
    {
        _scale = zoomTarget.localScale;
        _padding = image.raycastPadding;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (interactable == false) return;
        if (activePointerId != null && activePointerId != eventData.pointerId) return;
        activePointerId = null;

        if (overrideSeOrNull != null) SoundPlayer.PlayOneShot(overrideSeOrNull);
        else if (se != SeEnum.None) se.PlayOneShotAsync();
        onClick.OnNext(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable == false) return;

        if (activePointerId != null && activePointerId != eventData.pointerId) return;
        activePointerId = eventData.pointerId;

        if (zoomFlag)
        {
            if (needAdjustmentPaddingOnZoom)
            {
                // 押下中の縮小分だけ判定を拡大
                var p1 = _padding / ZoomRate;
                var p2 = zoomTarget.sizeDelta * (1f - ZoomRate) / 2f;

                _padding = image.raycastPadding;
                image.raycastPadding = new Vector4(p1.x - p2.x, p1.y - p2.y, p1.z - p2.x, p1.w - p2.y);
            }
            zoomTarget.localScale *= ZoomRate;
        }
    }

    public void OnPointerExit(PointerEventData eventData) => OnPointerUp(eventData);
    public void OnPointerUp(PointerEventData eventData)
    {
        if (activePointerId == eventData.pointerId) activePointerId = null;
        if (zoomFlag)
        {
            if (needAdjustmentPaddingOnZoom) image.raycastPadding = _padding;
            zoomTarget.localScale = _scale;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (image == null)
        {
            TryGetComponent(out image);
            image.raycastTarget = true;
            image.raycastPadding = Vector4.one * DefaultRaycastPaddingMargin;

            name = name.ToLower();
            if (SelectSeKeywords.Any(k => k.Contains(name))) se = SeEnum.Select;
            else if(CancelSeKeywords.Any(k => k.Contains(name))) se = SeEnum.Cancel;
            else if (ToggleSeKeywords.Any(k => k.Contains(name))) se = SeEnum.Toggle;
            else se = SeEnum.Choice;
        }
        if (zoomTarget == null) image?.TryGetComponent(out zoomTarget);
        if (image != null && zoomTarget != null)
        {
            needAdjustmentPaddingOnZoom = zoomTarget
                .GetComponentsInChildren<Transform>()
                .FirstOrDefault(t => t == image.transform);
        }
    }
#endif
}
