using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 격자 경로 추적 퍼즐을 관리합니다.
///  - Start 시 A에서 화살표 방향을 따라 노드를 순서대로 밝히며 진행
///  - 정확히 requiredCount개의 노드를 거쳐 B에 도달해야 성공
///  - 개수 초과/부족 → 실패, 이미 방문한 칸 재방문(루프) → 실패
///  - Assemble 모드에서 빈 칸 클릭 시 노드 생성
/// </summary>
public class PathPuzzleManager : MonoBehaviour
{
    [Header("격자 크기")]
    public int rows = 4;
    public int cols = 4;

    [Header("스테이지 설정")]
    [Tooltip("A → B 까지 거쳐야 하는 노드 개수 (A 포함)")]
    public int requiredCount = 6;

    [Header("진행 연출")]
    public float stepDelay = 0.35f;   // 노드 하나가 밝아지는 간격(초)

    [Header("참조")]
    [Tooltip("씬의 모든 GridSlot. 비워두면 자식에서 자동 수집")]
    public List<GridSlot> slots = new();
    public GridNode nodePrefab;             // Assemble 모드에서 생성할 노드 프리팹
    [Tooltip("상단 TRACE SLOTS 칸들 (왼쪽부터). 진행 수만큼 채워짐")]
    public List<Image> traceSlots = new();
    public Color traceFilledColor = new Color(0.35f, 0.75f, 0.95f);
    public Color traceEmptyColor  = new Color(0.15f, 0.15f, 0.18f);

    [Header("이벤트")]
    public UnityEvent onSuccess;
    public UnityEvent<string> onFail;    // 실패 사유 전달

    // ── 내부 ────────────────────────────────────────────────────────
    Dictionary<Vector2Int, GridSlot> grid = new();
    Vector2Int startCell, goalCell;
    bool isRunning;
    bool ownsInteractionLock;
    bool isSolved;

    public bool IsRunning => isRunning;
    public bool IsSolved => isSolved;
    public bool CanEdit => !isRunning && !isSolved;

    void Awake()
    {
        if (slots.Count == 0)
            slots.AddRange(GetComponentsInChildren<GridSlot>());

        BuildGrid();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        isRunning = false;
        ReleaseInteractionLock();
    }

    void BuildGrid()
    {
        grid.Clear();
        foreach (var s in slots)
        {
            var key = new Vector2Int(s.row, s.col);
            grid[key] = s;
            if (s.isStart) startCell = key;
            if (s.isGoal)  goalCell  = key;
            // 칸에 이미 노드가 자식으로 있으면 등록
            if (s.currentNode == null)
            {
                var n = s.GetComponentInChildren<GridNode>();
                if (n != null) { s.currentNode = n; n.row = s.row; n.col = s.col; }
            }
        }
    }

    // ── Assemble: 노드 생성 ─────────────────────────────────────────
    public void SpawnNodeAt(GridSlot slot)
    {
        if (nodePrefab == null)
        {
            Debug.LogWarning("[PathPuzzle] nodePrefab이 비어 있습니다.");
            return;
        }
        var node = Instantiate(nodePrefab, slot.transform);
        var nrt  = node.transform as RectTransform;
        nrt.anchoredPosition = Vector2.zero;          // 칸 중앙
        node.row = slot.row;
        node.col = slot.col;
        slot.currentNode = node;
        Debug.Log($"[PathPuzzle] 노드 생성 @ ({slot.row},{slot.col})");
    }

    // ── Start: 경로 추적 시작 (Start 버튼 OnClick에 연결) ────────────
    public void StartTrace()
    {
        if (isRunning || isSolved) return;
        StopAllCoroutines();
        StartCoroutine(TraceRoutine());
    }

    IEnumerator TraceRoutine()
    {
        isRunning = true;
        AcquireInteractionLock();
        ResetVisuals();

        var visited = new HashSet<Vector2Int>();
        Vector2Int cur = startCell;
        int count = 0;
        bool fail = false;
        string reason = "";

        while (true)
        {
            // 도착점 도달
            if (cur == goalCell) break;

            // 루프 검사 (이미 방문한 칸 재진입)
            if (visited.Contains(cur)) { fail = true; reason = "루프 발생 — 같은 칸 재방문"; break; }
            visited.Add(cur);

            // 현재 칸의 노드
            GridNode node = NodeAt(cur);
            if (node == null) { fail = true; reason = $"경로 끊김 — ({cur.x},{cur.y})에 노드 없음"; break; }

            // 밝히기 + 카운트
            node.SetLit(true);
            count++;
            FillTraceSlot(count);
            Debug.Log($"[PathPuzzle] {count}번째 노드 점등 @ ({cur.x},{cur.y}) 방향:{node.direction}");
            yield return new WaitForSeconds(stepDelay);

            // 개수 초과 즉시 실패
            if (count > requiredCount) { fail = true; reason = $"노드 개수 초과 (> {requiredCount})"; break; }

            // 다음 칸으로 이동
            Vector2Int next = cur + NodeGridDelta(node.direction);
            if (!grid.ContainsKey(next)) { fail = true; reason = "격자 밖으로 진행"; break; }
            cur = next;
        }

        // 결과 판정
        if (!fail)
        {
            if (count == requiredCount)
            {
                Debug.Log($"[PathPuzzle] ★ 성공! 정확히 {requiredCount}개 노드로 B 도달");
                MarkSolved();
                onSuccess?.Invoke();
            }
            else
            {
                reason = count < requiredCount
                    ? $"노드 개수 부족 ({count} < {requiredCount})"
                    : $"노드 개수 초과 ({count} > {requiredCount})";
                FailLog(reason);
            }
        }
        else FailLog(reason);

        isRunning = false;
        ReleaseInteractionLock();
    }

    void FailLog(string reason)
    {
        Debug.Log($"[PathPuzzle] ✗ 실패 — {reason}");
        isRunning = false;
        ReleaseInteractionLock();
        onFail?.Invoke(reason);
    }

    public void MarkSolved()
    {
        isSolved = true;
        isRunning = false;
        ReleaseInteractionLock();
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────
    void AcquireInteractionLock()
    {
        if (ownsInteractionLock)
            return;

        ownsInteractionLock = true;
        SkillInteractionLock.Push();
    }

    void ReleaseInteractionLock()
    {
        if (!ownsInteractionLock)
            return;

        ownsInteractionLock = false;
        SkillInteractionLock.Pop();
    }

    GridNode NodeAt(Vector2Int cell)
    {
        return grid.TryGetValue(cell, out var s) ? s.currentNode : null;
    }

    // GridNode.Delta는 (row, col) 순서이므로 그대로 사용
    Vector2Int NodeGridDelta(GridNode.Dir d) => GridNode.Delta(d);

    void ResetVisuals()
    {
        foreach (var s in slots)
            if (s.currentNode != null) s.currentNode.SetLit(false);
        for (int i = 0; i < traceSlots.Count; i++)
            if (traceSlots[i] != null) traceSlots[i].color = traceEmptyColor;
    }

    void FillTraceSlot(int count)
    {
        int idx = count - 1;
        if (idx >= 0 && idx < traceSlots.Count && traceSlots[idx] != null)
            traceSlots[idx].color = traceFilledColor;
    }

    // ── 에디터 디버그 ───────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("경로 추적 테스트 실행")]
    void EditorRun() => StartTrace();
#endif
}
