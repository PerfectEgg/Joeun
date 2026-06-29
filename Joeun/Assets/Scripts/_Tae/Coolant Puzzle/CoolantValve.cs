using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 노브에 의해 구동되는 밸브입니다. (직접 조작 불가, 매니저가 회전시킴)
///  - Supply : 90도 스텝, 가로(0/180) = ON
///  - Return : 120도 스텝, 0도 = ON
/// </summary>
public class CoolantValve : MonoBehaviour
{
    public enum Kind { Supply, Return }

    [Header("종류 / 식별")]
    public Kind kind;
    public int  index;          // 0~3  (S1=0 ... S4=3 / R1=0 ... R4=3)

    [Header("참조")]
    public RectTransform handle;   // 회전시킬 손잡이(라인) 이미지
    public Image ring;             // ON일 때 강조할 외곽 링 (선택)
    public Image lineImage;        // 라인 색 변경용 (선택)

    [Header("색상")]
    public Color onLineColor   = new Color(0.17f, 0.77f, 0.94f);  // 하늘색
    public Color offLineColor  = new Color(0.80f, 0.27f, 0.24f);  // 빨강
    public Color ringOnColor   = new Color(0.17f, 0.82f, 0.48f);  // 초록
    public Color ringOffColor  = new Color(0f, 0f, 0f, 0f);       // 투명

    [Header("연출")]
    public float rotateAnimDuration = 0.2f;

    public float CurrentAngle { get; private set; }   // 논리 각도 (도)
    Coroutine rotCo;

    /// <summary>회전 스텝 (Supply=90, Return=120)</summary>
    public float StepAngle => kind == Kind.Supply ? 90f : 120f;

    /// <summary>현재 ON 상태인지</summary>
    public bool IsOn
    {
        get
        {
            float a = Norm(CurrentAngle);
            return kind == Kind.Supply ? (a % 180f == 0f)   // 0 / 180
                                       : (a == 0f);          // 0 (120/240 = OFF)
        }
    }

    public void Init(float startAngle)
    {
        CurrentAngle = startAngle;
        ApplyVisualImmediate();
    }

    /// <summary>한 스텝 회전 (매니저가 호출)</summary>
    public void Step()
    {
        CurrentAngle += StepAngle;
        if (rotCo != null) StopCoroutine(rotCo);
        rotCo = StartCoroutine(SmoothRotate(CurrentAngle));
        RefreshColors();
    }

    System.Collections.IEnumerator SmoothRotate(float target)
    {
        if (handle == null) yield break;
        float start = handle.localEulerAngles.z;
        float delta = Mathf.DeltaAngle(start, -target);   // 화면 시계방향 = z 음수
        float t = 0f;
        while (t < rotateAnimDuration)
        {
            t += Time.deltaTime;
            handle.localEulerAngles = new Vector3(0, 0, start + delta * (t / rotateAnimDuration));
            yield return null;
        }
        handle.localEulerAngles = new Vector3(0, 0, -target);
    }

    void ApplyVisualImmediate()
    {
        if (handle != null) handle.localEulerAngles = new Vector3(0, 0, -CurrentAngle);
        RefreshColors();
    }

    public void RefreshColors()
    {
        bool on = IsOn;
        if (lineImage != null) lineImage.color = on ? onLineColor : offLineColor;
        if (ring != null)      ring.color      = on ? ringOnColor : ringOffColor;
    }

    /// <summary>전원 꺼짐(미가동) 표시용 — Return이 Phase1에서 흐리게</summary>
    public void SetPowered(bool powered)
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 1f;
    }

    static float Norm(float a) => ((a % 360f) + 360f) % 360f;
}
