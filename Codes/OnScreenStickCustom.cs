using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

[AddComponentMenu("Input/On-Screen Stick Custom")]
public class OnScreenStickCustom : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField]
    private string m_ControlPath;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }


    [SerializeField] private Image bgImage;
    [SerializeField] private Image stickImage;
    [SerializeField] private float range = 10f;
    [SerializeField, Range(0f, 1f)] private float deadZone = 0.1f;

    private Transform bgImageTransform, stickImageTransform;


    private void Awake()
    {
        bgImageTransform = bgImage.transform;
        stickImageTransform = stickImage.transform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        bgImageTransform.position = eventData.position;
        stickImageTransform.position = eventData.position;
        bgImage.enabled = true;
        stickImage.enabled = true;
    }
    public void OnDrag(PointerEventData eventData)
    {
        var currentRange = range * bgImageTransform.lossyScale.x;
        var vector = (Vector3)eventData.position - bgImageTransform.position;
        var magnitude = vector.magnitude;

        if (magnitude < currentRange * deadZone) vector = Vector3.zero;
        else if (magnitude > currentRange) vector *= currentRange / magnitude;

        stickImageTransform.position = bgImage.transform.position + vector;

        vector /= currentRange;
        SendValueToControl((Vector2)vector);
    }
    public void OnPointerUp(PointerEventData data)
    {
        SendValueToControl(Vector2.zero);
        bgImage.enabled = false;
        stickImage.enabled = false;
    }
}
