using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 가운데 CONTROL 노브(1~4). 클릭하면 매니저에 조작을 알립니다.
/// 클릭 시 자신도 90도 회전(피드백)합니다.
/// </summary>
public class CoolantKnob : MonoBehaviour, IPointerClickHandler
{
    [Header("식별")]
    public int number;          // 1~4

    [Header("참조")]
    public RectTransform handle;   // 회전시킬 라인 이미지

    [Header("연출")]
    public float rotateAnimDuration = 0.2f;

    float angle;
    Coroutine rotCo;
    CoolantPuzzleManager manager;

    void Awake()
    {
        manager = GetComponentInParent<CoolantPuzzleManager>();
    }

    public void OnPointerClick(PointerEventData ev)
    {
        manager?.OperateKnob(number);
        SpinFeedback();
    }

    void SpinFeedback()
    {
        angle += 90f;
        if (handle == null) return;
        if (rotCo != null) StopCoroutine(rotCo);
        rotCo = StartCoroutine(Spin(angle));
    }

    System.Collections.IEnumerator Spin(float target)
    {
        float start = handle.localEulerAngles.z;
        float delta = Mathf.DeltaAngle(start, -target);
        float t = 0f;
        while (t < rotateAnimDuration)
        {
            t += Time.deltaTime;
            handle.localEulerAngles = new Vector3(0, 0, start + delta * (t / rotateAnimDuration));
            yield return null;
        }
        handle.localEulerAngles = new Vector3(0, 0, -target);
    }
}
