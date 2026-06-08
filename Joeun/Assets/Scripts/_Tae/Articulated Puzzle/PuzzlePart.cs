using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 드래그 가능한 퍼즐 파츠입니다.
/// IBeginDragHandler / IDragHandler / IEndDragHandler 를 사용하므로
/// Canvas에 GraphicRaycaster, EventSystem이 반드시 있어야 합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PuzzlePart : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("파츠 정보")]
    public string partId;          // "A", "B", "C", "D"
    public int    orderIndex;      // 정답 순서 (A=0, B=1, C=2, D=3)

    [Header("스냅")]
    public float snapAnimDuration = 0.12f;   // 스냅 시 이동 애니메이션 시간

    // 이 파츠가 가진 연결점 목록 (Inspector 자동 수집)
    [HideInInspector] public List<ConnectorPoint> connectors = new();

    // ── 내부 상태 ──────────────────────────────────────────────────
    RectTransform   rt;
    Canvas          rootCanvas;
    Vector2         dragOffset;          // 클릭 위치 보정값
    Vector2         originPos;           // 드래그 시작 위치 (스냅 실패 시 복귀용, 필요하면 활성화)
    bool            isDragging;
    ConnectorPoint  previewTarget;       // 드래그 중 근접 강조 대상
    Coroutine       snapCoroutine;

    void Awake()
    {
        rt          = GetComponent<RectTransform>();
        rootCanvas  = GetComponentInParent<Canvas>();
        connectors.AddRange(GetComponentsInChildren<ConnectorPoint>());
    }

    // ── 드래그 이벤트 ───────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData ev)
    {
        // 이미 연결된 파츠는 드래그 시 연결 해제
        foreach (var cp in connectors) cp.Unlink();

        PuzzleManager.Instance?.NotifyPartMoved();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform,
            ev.position,
            ev.pressEventCamera,
            out Vector2 localPos);

        dragOffset = rt.anchoredPosition - localPos;
        originPos  = rt.anchoredPosition;
        isDragging = true;

        // 드래그 중인 파츠를 최상단으로
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData ev)
    {
        if (!isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform,
            ev.position,
            ev.pressEventCamera,
            out Vector2 localPos);

        rt.anchoredPosition = localPos + dragOffset;

        // 근접 강조 미리보기
        UpdateSnapPreview();
    }

    public void OnEndDrag(PointerEventData ev)
    {
        isDragging = false;
        ClearPreview();
        TrySnap();
    }

    // ── 스냅 처리 ───────────────────────────────────────────────────

    /// <summary>드래그 중 가장 가까운 연결 가능한 점을 초록색으로 강조</summary>
    void UpdateSnapPreview()
    {
        var (myPt, target, _) = FindBestSnap();

        // 이전 강조 해제
        if (previewTarget != null && previewTarget != target)
        {
            previewTarget.SetVisual(previewTarget.idleColor);
            foreach (var cp in connectors)
                cp.SetVisual(cp.idleColor);
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
        foreach (var cp in connectors)
            if (!cp.IsLinked) cp.SetVisual(cp.idleColor);
    }

    /// <summary>드래그 종료 시 조건을 만족하면 스냅</summary>
    void TrySnap()
    {
        var (myPt, target, snapWorldPos) = FindBestSnap();
        if (myPt == null || target == null) return;

        // 스냅 위치 계산: target 위치에 myPt 가 정확히 겹치도록 파츠 이동
        Vector2 delta = (Vector2)target.transform.position - (Vector2)myPt.transform.position;

        // anchoredPosition 기준으로 변환
        Vector2 scaleFactor = rootCanvas.scaleFactor * Vector2.one;
        Vector2 newAnchored = rt.anchoredPosition + delta / rootCanvas.scaleFactor;

        // 연결 처리 (위치 이동 전에 미리 등록)
        myPt.LinkTo(target);
        PuzzleManager.Instance?.NotifyConnected(this, myPt, target);

        // 부드러운 슬라이드 이동
        if (snapCoroutine != null) StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(SmoothMove(newAnchored));
    }

    /// <summary>이 파츠의 연결점 중 가장 가까운 (내 점, 상대 점, 스냅 월드위치) 쌍 반환</summary>
    (ConnectorPoint myPt, ConnectorPoint target, Vector2 worldPos) FindBestSnap()
    {
        ConnectorPoint bestMy  = null;
        ConnectorPoint bestTgt = null;
        float          bestDist = float.MaxValue;

        // 씬의 모든 연결점 순회
        var allPoints = FindObjectsByType<ConnectorPoint>(FindObjectsSortMode.None);
        foreach (var myPt in connectors)
        {
            if (myPt.IsLinked) continue;
            foreach (var other in allPoints)
            {
                if (!myPt.CanConnectTo(other)) continue;
                float dist = Vector2.Distance(myPt.WorldPosition(), other.WorldPosition());
                if (dist < myPt.snapRadius && dist < bestDist)
                {
                    bestDist = dist;
                    bestMy   = myPt;
                    bestTgt  = other;
                }
            }
        }
        return (bestMy, bestTgt, bestTgt?.WorldPosition() ?? Vector2.zero);
    }

    IEnumerator SmoothMove(Vector2 targetPos)
    {
        Vector2 start = rt.anchoredPosition;
        float   t     = 0f;
        while (t < snapAnimDuration)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(start, targetPos, t / snapAnimDuration);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
    }
}
