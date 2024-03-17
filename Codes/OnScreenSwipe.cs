using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using System.Collections.Generic;

[AddComponentMenu("Input/On-Screen Swipe")]
public class OnScreenSwipe : OnScreenControl, IPointerUpHandler, IDragHandler
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
    [SerializeField] private double detectionDuration = 0.1d;
    // [SerializeField] private bool normalized = true;

    private Queue<(Vector2 pos, double time)> dragQueue = new();

    public void OnPointerUp(PointerEventData data)
    {
        CleanQueue();
        if (dragQueue.TryPeek(out var item) && (item.pos - data.position).sqrMagnitude >= minRange * minRange)
        {
            SendValueToControl(1f);
            SendValueToControl(0f);
            // var vector = data.position - item.pos;
            // SendValueToControl(normalized ? vector.normalized : vector);
            // SendValueToControl(Vector2.zero);
        }
        dragQueue.Clear();
    }
    public void OnDrag(PointerEventData eventData)
    {
        dragQueue.Enqueue((eventData.position, Time.realtimeSinceStartupAsDouble));
        CleanQueue();
    }

    private void CleanQueue()
    {
        while (dragQueue.TryPeek(out var item))
        {
            if (Time.realtimeSinceStartupAsDouble - item.time > detectionDuration) dragQueue.Dequeue();
            else break;
        }
    }
}
