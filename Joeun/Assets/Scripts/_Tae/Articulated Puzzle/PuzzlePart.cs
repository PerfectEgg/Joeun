using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 드래그 + 90도 회전이 가능한 퍼즐 파츠입니다.
/// 마우스를 올린 파츠가 'Active'가 되며, Q키 또는 UI버튼으로 회전합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PuzzlePart : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("파츠 정보")]
    public string partId;          // "A", "B", "C", "D"
    public int    orderIndex;      // 정답 순서 (A=0, B=1, C=2, D=3)

    [Header("스냅 / 회전")]
    public float snapAnimDuration   = 0.12f;
    public float rotateAnimDuration = 0.12f;   // 회전 애니메이션 시간
    public KeyCode rotateKey        = KeyCode.Q;

    [HideInInspector] public List<ConnectorPoint> connectors = new();

    /// <summary>현재 마우스가 올라가 있거나 드래그 중인 파츠 (Q 회전 대상)</summary>
    public static PuzzlePart Active { get; private set; }

    // ── 내부 상태 ──────────────────────────────────────────────────
    RectTransform   rt;
    Canvas          rootCanvas;
    Vector2         dragOffset;
    bool            isDragging;
    ConnectorPoint  previewTarget;
    Coroutine       snapCoroutine;
    Coroutine       rotateCoroutine;

    void Awake()
    {
        rt         = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        connectors.AddRange(GetComponentsInChildren<ConnectorPoint>());
    }

    void Update()
    {
        // Q키로 현재 Active 파츠 회전
        if (Active == this && Input.GetKeyDown(rotateKey))
            Rotate90();
    }

    // ── 호버 (Active 지정) ──────────────────────────────────────────
    public void OnPointerEnter(PointerEventData ev) { Active = this; }
    public void OnPointerExit(PointerEventData ev)  { if (Active == this) Active = null; }

    // ── 회전 ────────────────────────────────────────────────────────

    /// <summary>이 파츠를 90도 회전합니다. (Q키, UI버튼 등에서 호출)</summary>
    public void Rotate90()
    {
        // 회전하면 기존 연결은 해제 (다시 맞춰 끼우는 가믹)
        foreach (var cp in connectors) cp.Unlink();
        PuzzleManager.Instance?.NotifyPartMoved();

        float targetZ = rt.eulerAngles.z - 90f;   // 시계방향 90도
        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        rotateCoroutine = StartCoroutine(SmoothRotate(targetZ));
    }

    IEnumerator SmoothRotate(float targetZ)
    {
        float startZ = rt.eulerAngles.z;
        // 최단 경로로 보간
        float delta  = Mathf.DeltaAngle(startZ, targetZ);
        float t = 0f;
        while (t < rotateAnimDuration)
        {
            t += Time.deltaTime;
            float z = startZ + delta * (t / rotateAnimDuration);
            rt.localEulerAngles = new Vector3(0f, 0f, z);
            yield return null;
        }
        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Round(targetZ / 90f) * 90f);
    }

    // ── 드래그 ──────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData ev)
    {
        Active = this;
        foreach (var cp in connectors) cp.Unlink();
        PuzzleManager.Instance?.NotifyPartMoved();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform, ev.position, ev.pressEventCamera, out Vector2 localPos);

        dragOffset = rt.anchoredPosition - localPos;
        isDragging = true;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData ev)
    {
        if (!isDragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform, ev.position, ev.pressEventCamera, out Vector2 localPos);
        rt.anchoredPosition = localPos + dragOffset;
        UpdateSnapPreview();
    }

    public void OnEndDrag(PointerEventData ev)
    {
        isDragging = false;
        ClearPreview();
        TrySnap();
    }

    // ── 스냅 처리 ───────────────────────────────────────────────────

    void UpdateSnapPreview()
    {
        var (myPt, target, _) = FindBestSnap();
        if (previewTarget != null && previewTarget != target)
        {
            previewTarget.SetVisual(previewTarget.idleColor);
            foreach (var cp in connectors) if (!cp.IsLinked) cp.SetVisual(cp.idleColor);
        }
        previewTarget = target;
        if (target != null)
        {
            target.SetVisual(target.nearColor);
            if (myPt != null) myPt.SetVisual(myPt.nearColor);
        }
    }

    void ClearPreview()
    {
        if (previewTarget != null)
        {
            previewTarget.SetVisual(previewTarget.idleColor);
            previewTarget = null;
        }
        foreach (var cp in connectors) if (!cp.IsLinked) cp.SetVisual(cp.idleColor);
    }

    void TrySnap()
    {
        var (myPt, target, _) = FindBestSnap();
        if (myPt == null || target == null) return;

        Vector2 delta = (Vector2)target.transform.position - (Vector2)myPt.transform.position;
        Vector2 newAnchored = rt.anchoredPosition + delta / rootCanvas.scaleFactor;

        myPt.LinkTo(target);
        PuzzleManager.Instance?.NotifyConnected(this, myPt, target);

        if (snapCoroutine != null) StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(SmoothMove(newAnchored));
    }

    (ConnectorPoint myPt, ConnectorPoint target, Vector2 worldPos) FindBestSnap()
    {
        ConnectorPoint bestMy = null, bestTgt = null;
        float bestDist = float.MaxValue;

        var allPoints = FindObjectsByType<ConnectorPoint>(FindObjectsSortMode.None);
        foreach (var myPt in connectors)
        {
            if (myPt.IsLinked) continue;
            foreach (var other in allPoints)
            {
                if (!myPt.CanConnectTo(other)) continue;   // 마주 봄 + 미연결 검사 포함
                float dist = Vector2.Distance(myPt.WorldPosition(), other.WorldPosition());
                if (dist < myPt.snapRadius && dist < bestDist)
                {
                    bestDist = dist; bestMy = myPt; bestTgt = other;
                }
            }
        }
        return (bestMy, bestTgt, bestTgt?.WorldPosition() ?? Vector2.zero);
    }

    IEnumerator SmoothMove(Vector2 targetPos)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < snapAnimDuration)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(start, targetPos, t / snapAnimDuration);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
    }
}