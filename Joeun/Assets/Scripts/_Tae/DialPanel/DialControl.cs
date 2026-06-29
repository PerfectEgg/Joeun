using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 좌우 드래그로 값을 조절하는 다이얼입니다.
/// 빨간 막대가 그려진 다이얼 스프라이트를 회전시킵니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DialControl : MonoBehaviour, IDragHandler
{
    [Header("값 범위")]
    public float minValue = 0f;
    public float maxValue = 500f;
    public float value    = 250f;
    [Tooltip("값이 이 단위로 스냅됩니다 (0이면 스냅 없음)")]
    public float step     = 5f;

    [Header("드래그 감도")]
    [Tooltip("이 픽셀만큼 가로로 끌면 (max-min) 전체를 이동")]
    public float dragPixelsForFullRange = 150f;

    [Header("다이얼 회전")]
    [Tooltip("회전시킬 다이얼 이미지. 비우면 이 오브젝트 자신을 회전")]
    public RectTransform dialVisual;
    public float angleMin = -135f; // 최소값일 때 각도
    public float angleMax = 135f;  // 최대값일 때 각도

    [Header("이벤트")]
    public UnityEvent<float> onValueChanged;

    RectTransform target;

    void Awake()
    {
        target = dialVisual != null ? dialVisual : (RectTransform)transform;
    }

    void Start()
    {
        ApplyClampAndSnap();
        Refresh();
    }

    public void OnDrag(PointerEventData ev)
    {
        // 가로 이동량(dx)을 값 변화로 변환 (오른쪽 = 증가)
        float dx = ev.delta.x;
        float range = maxValue - minValue;
        value += dx * range / Mathf.Max(1f, dragPixelsForFullRange);
        ApplyClampAndSnap();
        Refresh();
    }

    void ApplyClampAndSnap()
    {
        if (step > 0f)
            value = Mathf.Round(value / step) * step;
        value = Mathf.Clamp(value, minValue, maxValue);
    }

    void Refresh()
    {
        // 다이얼 이미지 자체를 회전 (화면 시계방향 = z 음수)
        float ratio = Mathf.InverseLerp(minValue, maxValue, value);
        float angle = Mathf.Lerp(angleMin, angleMax, ratio);
        target.localEulerAngles = new Vector3(0f, 0f, -angle);

        onValueChanged?.Invoke(value);
    }

    /// <summary>외부에서 값을 직접 설정할 때</summary>
    public void SetValue(float v)
    {
        value = v;
        ApplyClampAndSnap();
        Refresh();
    }
}