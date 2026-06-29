using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TransformerDialControl2D : MonoBehaviour, IDraggable
{
    [Header("Value")]
    [SerializeField] private float minValue;
    [SerializeField] private float maxValue = 9f;
    [SerializeField] private float value;
    [SerializeField] private float step = 1f;

    [Header("Rotation")]
    [SerializeField] private float angleMin = -135f;
    [SerializeField] private float angleMax = 135f;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform center;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged;

    private float currentAngle;
    private float dragAngle;
    private float lastPointerAngle;
    private bool dragging;

    public event Action<TransformerDialControl2D, float> ValueChanged;

    public Vector2 OriginPosition => transform.position;
    public float Value => value;

    private Transform VisualRoot => visualRoot != null ? visualRoot : transform;
    private Transform Center => center != null ? center : VisualRoot;

    private void Awake()
    {
        SyncAngleFromValue();
    }

    private void OnEnable()
    {
        SyncAngleFromValue();
    }

    public void SetValue(float newValue)
    {
        value = newValue;
        SyncAngleFromValue();
    }

    public void OnDragStart()
    {
        BeginDrag(GetMouseWorldPosition());
    }

    public void OnDrag(Vector2 currentMousePosition)
    {
        DragTo(currentMousePosition);
    }

    public void OnDragEnd()
    {
        EndDrag();
    }

    private void OnMouseDown()
    {
        BeginDrag(GetMouseWorldPosition());
    }

    private void OnMouseDrag()
    {
        DragTo(GetMouseWorldPosition());
    }

    private void OnMouseUp()
    {
        EndDrag();
    }

    private void BeginDrag(Vector2 mouseWorldPosition)
    {
        if (dragging)
            return;

        dragging = true;
        dragAngle = currentAngle;
        lastPointerAngle = PointerAngle(mouseWorldPosition);
    }

    private void DragTo(Vector2 mouseWorldPosition)
    {
        if (!dragging)
            return;

        float pointerAngle = PointerAngle(mouseWorldPosition);
        float deltaAngle = Mathf.DeltaAngle(lastPointerAngle, pointerAngle);
        lastPointerAngle = pointerAngle;

        dragAngle = Mathf.Clamp(dragAngle + deltaAngle, angleMin, angleMax);
        ApplyValueFromAngle(dragAngle);
    }

    private void EndDrag()
    {
        dragging = false;
    }

    private float PointerAngle(Vector2 mouseWorldPosition)
    {
        Vector2 centerPosition = Center.position;
        Vector2 direction = mouseWorldPosition - centerPosition;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void SyncAngleFromValue()
    {
        value = SnapValue(value);
        currentAngle = AngleFromValue(value);
        RenderAngle(currentAngle);
        NotifyValueChanged();
    }

    private void ApplyValueFromAngle(float angle)
    {
        float previousValue = value;
        value = SnapValue(ValueFromAngle(angle));
        currentAngle = AngleFromValue(value);
        RenderAngle(currentAngle);

        if (!Mathf.Approximately(previousValue, value))
            NotifyValueChanged();
    }

    private float SnapValue(float rawValue)
    {
        float clamped = Mathf.Clamp(rawValue, minValue, maxValue);
        if (step <= 0f)
            return clamped;

        float snapped = Mathf.Round((clamped - minValue) / step) * step + minValue;
        return Mathf.Clamp(snapped, minValue, maxValue);
    }

    private float ValueFromAngle(float angle)
    {
        if (Mathf.Approximately(angleMin, angleMax))
            return minValue;

        float ratio = Mathf.InverseLerp(angleMin, angleMax, angle);
        return Mathf.Lerp(minValue, maxValue, ratio);
    }

    private float AngleFromValue(float targetValue)
    {
        if (Mathf.Approximately(minValue, maxValue))
            return angleMin;

        float ratio = Mathf.InverseLerp(minValue, maxValue, targetValue);
        return Mathf.Lerp(angleMin, angleMax, ratio);
    }

    private void RenderAngle(float angle)
    {
        Transform target = VisualRoot;
        if (target == null)
            return;

        target.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void NotifyValueChanged()
    {
        onValueChanged?.Invoke(value);
        ValueChanged?.Invoke(this, value);
    }

    private static Vector2 GetMouseWorldPosition()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return Vector2.zero;

        return camera.ScreenToWorldPoint(Input.mousePosition);
    }
}
