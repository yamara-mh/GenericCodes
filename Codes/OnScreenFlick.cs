using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

// Note: You can handle the flick direction by uncommenting or replacing it.
[AddComponentMenu("Input/On-Screen Flick")]
public class OnScreenFlick : OnScreenControl, IPointerDownHandler, IPointerUpHandler
{
    [InputControl(layout = "Button")]
    // [InputControl(layout = "Vector2")]
    [SerializeField]
    private string m_ControlPath;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }


    [SerializeField] private float minRange = 50f;
    [SerializeField] private double maxDuration = 0.25d;
    // [SerializeField] private bool normalized = true;

    private double onPointerDownTime;

    public void OnPointerDown(PointerEventData eventData) => onPointerDownTime = Time.realtimeSinceStartupAsDouble;
    public void OnPointerUp(PointerEventData data)
    {
        if (Time.realtimeSinceStartupAsDouble - onPointerDownTime <= maxDuration &&
            (data.pressPosition - data.position).sqrMagnitude >= minRange * minRange)
        {
            SendValueToControl(1f);
            SendValueToControl(0f);
            // var vector = data.position - data.pressPosition;
            // SendValueToControl(normalized ? vector.normalized : vector);
            // SendValueToControl(Vector2.zero);
        }
    }
}
