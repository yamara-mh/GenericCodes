using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

[AddComponentMenu("Input/On-Screen Tap")]
public class OnScreenTap : OnScreenControl, IPointerDownHandler, IPointerUpHandler
{
    [InputControl(layout = "Button")]
    [SerializeField]
    private string m_ControlPath;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    [SerializeField] private float maxRange = 25f;
    [SerializeField] private double maxDuration = 0.2d;

    private double onPointerDownTime;

    public void OnPointerDown(PointerEventData eventData) => onPointerDownTime = Time.realtimeSinceStartupAsDouble;
    public void OnPointerUp(PointerEventData data)
    {
        if (Time.realtimeSinceStartupAsDouble - onPointerDownTime <= maxDuration &&
            (data.pressPosition - data.position).sqrMagnitude >= maxRange * maxRange)
        {
            SendValueToControl(1f);
            SendValueToControl(0f);
        }
    }
}
