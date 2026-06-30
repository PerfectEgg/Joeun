using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 드래그 + 회전이 가능한 연결 퍼즐 파츠입니다.
/// 회전은 PuzzleModeManager가 Rotate 모드일 때 '클릭'으로 발동합니다.
/// (드래그는 모드와 무관하게 항상 가능 — 클릭과 드래그는 자동 구분됨)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PuzzlePart : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("파츠 정보")]
    public string partId;          // "A", "B", "C", "D"
    public int    orderIndex;      // 정답 순서 (A=0, B=1, C=2, D=3)

    [Header("스냅 / 회전")]
    public float snapAnimDuration   = 0.12f;
    public float rotateAnimDuration = 0.12f;

    [HideInInspector] public List<ConnectorPoint> connectors = new();

    // ── 내부 상태 ──────────────────────────────────────────────────
    RectTransform   rt;
    Canvas          rootCanvas;
    Vector2         dragOffset;
    bool            isDragging;
    ConnectorPoint  previewTarget;
    Coroutine       snapCoroutine;
    Coroutine       rotateCoroutine;
    RectTransform   dragBoundsRoot;
    readonly Vector3[] worldCorners = new Vector3[4];

    void Awake()
    {
        rt         = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        connectors.AddRange(GetComponentsInChildren<ConnectorPoint>());
    }

    // ── 클릭 → 회전 (Rotate 모드일 때만) ────────────────────────────
    public void OnPointerClick(PointerEventData ev)
    {
        // 드래그 동작 후에는 클릭으로 간주되지 않으므로 안전
        if (PuzzleModeManager.Instance != null && PuzzleModeManager.Instance.IsRotate)
            Rotate90();
    }

    // ── 회전 ────────────────────────────────────────────────────────
    public void Rotate90()
    {
        foreach (var cp in connectors) cp.Unlink();   // 회전 시 기존 연결 해제
        ConnectPuzzleManager.Instance?.NotifyPartMoved();

        float targetZ = rt.eulerAngles.z - 90f;        // 시계방향 90도
        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        rotateCoroutine = StartCoroutine(SmoothRotate(targetZ));
    }

    IEnumerator SmoothRotate(float targetZ)
    {
        float startZ = rt.eulerAngles.z;
        float delta  = Mathf.DeltaAngle(startZ, targetZ);
        float t = 0f;
        while (t < rotateAnimDuration)
        {
            t += Time.deltaTime;
            rt.localEulerAngles = new Vector3(0f, 0f, startZ + delta * (t / rotateAnimDuration));
            yield return null;
        }
        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Round(targetZ / 90f) * 90f);
    }

    // ── 드래그 ──────────────────────────────────────────────────────
    public void OnBeginDrag(PointerEventData ev)
    {
        foreach (var cp in connectors) cp.Unlink();
        ConnectPuzzleManager.Instance?.NotifyPartMoved();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform, ev.position, ev.pressEventCamera, out Vector2 localPos);

        dragOffset = rt.anchoredPosition - localPos;
        isDragging = true;
        // transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData ev)
    {
        if (!isDragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform, ev.position, ev.pressEventCamera, out Vector2 localPos);
        rt.anchoredPosition = ClampToParentRect(localPos + dragOffset);
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
        ConnectPuzzleManager.Instance?.NotifyConnected(this, myPt, target);

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
                if (!myPt.CanConnectTo(other)) continue;
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

    Vector2 ClampToParentRect(Vector2 targetPosition)
    {
        RectTransform parentRect = rt.parent as RectTransform;
        if (parentRect == null)
            return targetPosition;

        RectTransform boundsRoot = ResolveDragBoundsRoot();
        if (boundsRoot == null || boundsRoot == parentRect)
        {
            Rect parent = parentRect.rect;
            targetPosition.x = Mathf.Clamp(targetPosition.x, parent.xMin, parent.xMax);
            targetPosition.y = Mathf.Clamp(targetPosition.y, parent.yMin, parent.yMax);
            return targetPosition;
        }

        boundsRoot.GetWorldCorners(worldCorners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 local = parentRect.InverseTransformPoint(worldCorners[i]);
            minX = Mathf.Min(minX, local.x);
            maxX = Mathf.Max(maxX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxY = Mathf.Max(maxY, local.y);
        }

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        return targetPosition;
    }

    RectTransform ResolveDragBoundsRoot()
    {
        if (dragBoundsRoot != null)
            return dragBoundsRoot;

        Transform current = rt.parent;
        while (current != null)
        {
            RectTransform found = FindRectTransformByName(current, "DragBounds");
            if (found != null && found != rt)
            {
                dragBoundsRoot = found;
                return dragBoundsRoot;
            }

            current = current.parent;
        }

        return rt.parent as RectTransform;
    }

    RectTransform FindRectTransformByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName && root.TryGetComponent(out RectTransform rootRect))
            return rootRect;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform found = FindRectTransformByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}
