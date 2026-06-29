using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Display")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private Sprite blankSprite;
    [SerializeField] private List<SpriteRenderer> digitSlots = new List<SpriteRenderer>();
    [SerializeField] private bool padWithZeros = true;

    private float currentAngle;
    private float dragAngle;
    private float lastPointerAngle;
    private bool dragging;

    public event Action<TransformerDialControl2D, float> ValueChanged;

    public Vector2 OriginPosition => transform.position;
    public float Value => value;
    public int IntValue => Mathf.RoundToInt(value);
    public int DisplayValue => GetDisplayValue(Mathf.Max(1, digitSlots.Count));

    public bool MatchesAnswer(int answerValue)
    {
        return IntValue == answerValue || DisplayValue == answerValue;
    }

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

    public void Refresh()
    {
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
        dragAngle = currentAngle;
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

        float ratio = 1f - Mathf.InverseLerp(angleMin, angleMax, angle);
        return Mathf.Lerp(minValue, maxValue, ratio);
    }

    private float AngleFromValue(float targetValue)
    {
        if (Mathf.Approximately(minValue, maxValue))
            return angleMin;

        float ratio = 1f - Mathf.InverseLerp(minValue, maxValue, targetValue);
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
        RefreshDisplay();
        ValueChanged?.Invoke(this, value);
    }

    private void RefreshDisplay()
    {
        int slotCount = digitSlots.Count;
        if (slotCount == 0)
            return;

        int displayValue = GetDisplayValue(slotCount);
        string text = displayValue.ToString();

        if (text.Length > slotCount)
            text = text.Substring(text.Length - slotCount);

        int pad = slotCount - text.Length;

        for (int i = 0; i < slotCount; i++)
        {
            SpriteRenderer slot = digitSlots[i];
            if (slot == null)
                continue;

            if (i < pad)
            {
                slot.sprite = padWithZeros ? GetDigit(0) : blankSprite;
                slot.enabled = slot.sprite != null;
                continue;
            }

            int digit = text[i - pad] - '0';
            slot.sprite = GetDigit(digit);
            slot.enabled = slot.sprite != null;
        }
    }

    private Sprite GetDigit(int digit)
    {
        if (digit >= 0 && digit < digitSprites.Length)
            return digitSprites[digit];

        return null;
    }

    private int GetDisplayValue(int slotCount)
    {
        if (slotCount == 1 && step > 1f)
            return Mathf.Max(0, Mathf.RoundToInt(value / step));

        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    private static Vector2 GetMouseWorldPosition()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return Vector2.zero;

        return camera.ScreenToWorldPoint(Input.mousePosition);
    }
}
