using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;


[RequireComponent(typeof(RectTransform))]
public class DialControl : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("값 범위")]
    public float minValue = 0f;
    public float maxValue = 500f;
    public float value    = 250f;
    [Tooltip("값이 이 단위로 스냅됩니다 (0이면 스냅 없음)")]
    public float step     = 5f;

    [Header("다이얼 회전 범위")]
    public float angleMin = -135f;   // 최소값일 때 각도
    public float angleMax = 135f;    // 최대값일 때 각도

    [Header("참조")]
    [Tooltip("회전시킬 다이얼 이미지. 비우면 이 오브젝트 자신을 회전")]
    public RectTransform dialVisual;

    [Header("이벤트")]
    public UnityEvent<float> onValueChanged;

    RectTransform target;
    Canvas        canvas;
    Camera        uiCamera;

    // 현재 값이 매핑된 다이얼 각도(논리값). angleMin~angleMax 범위.
    float currentAngle;
    // 직전 프레임에 측정한 '마우스 각도'
    float lastPointerAngle;

    void Awake()
    {
        target   = dialVisual != null ? dialVisual : (RectTransform)transform;
        canvas   = GetComponentInParent<Canvas>();
        uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                   ? null : canvas?.worldCamera;
    }

    void Start()
    {
        // 현재 value를 각도로 환산해 초기 표시
        float ratio = Mathf.InverseLerp(minValue, maxValue, value);
        currentAngle = Mathf.Lerp(angleMin, angleMax, ratio);
        ApplyClampSnapAndRender();
    }

    public void OnBeginDrag(PointerEventData ev)
    {
        // 드래그 시작 시점의 마우스 각도를 기준점으로 저장
        lastPointerAngle = PointerAngle(ev);
    }

    public void OnDrag(PointerEventData ev)
    {
        float pointerAngle = PointerAngle(ev);

        // 이전 프레임 대비 각도 변화량 (-180~180으로 정규화해 경계 처리)
        float deltaAngle = Mathf.DeltaAngle(lastPointerAngle, pointerAngle);
        lastPointerAngle = pointerAngle;

        // 변화량만큼 다이얼 각도 누적
        currentAngle += deltaAngle;
        currentAngle = Mathf.Clamp(currentAngle, angleMin, angleMax);

        // 각도 → 값 환산
        float ratio = Mathf.InverseLerp(angleMin, angleMax, currentAngle);
        value = Mathf.Lerp(minValue, maxValue, ratio);

        ApplyClampSnapAndRender();
    }

    /// <summary>다이얼 중심 기준 마우스 포인터의 각도(도)</summary>
    float PointerAngle(PointerEventData ev)
    {
        // 다이얼 중심의 스크린 좌표
        Vector2 center = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        Vector2 dir = ev.position - center;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    void ApplyClampSnapAndRender()
    {
        // 값 스냅 & 제한
        if (step > 0f)
            value = Mathf.Round(value / step) * step;
        value = Mathf.Clamp(value, minValue, maxValue);

        // 스냅된 값을 다시 각도로 맞춰 시각화 (값과 막대 위치 일치)
        float ratio = Mathf.InverseLerp(minValue, maxValue, value);
        float shownAngle = Mathf.Lerp(angleMin, angleMax, ratio);

        // 다이얼 이미지 회전
        target.localEulerAngles = new Vector3(0f, 0f, shownAngle);

        onValueChanged?.Invoke(value);
    }

    /// <summary>외부에서 값을 직접 설정할 때</summary>
    public void SetValue(float v)
    {
        value = v;
        float ratio = Mathf.InverseLerp(minValue, maxValue, value);
        currentAngle = Mathf.Lerp(angleMin, angleMax, ratio);
        ApplyClampSnapAndRender();
    }
}