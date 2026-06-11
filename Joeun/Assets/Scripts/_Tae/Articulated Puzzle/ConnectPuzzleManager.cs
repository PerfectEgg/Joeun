using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 퍼즐 전체를 관리합니다.
///  - 연결 이벤트를 수신하고 A→B→C→D 순서 체인 검증
///  - 완료 여부를 Log 출력 & UnityEvent로 외부에 노출
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────────────────────────────
    public static PuzzleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 설정 ────────────────────────────────────────────────────────
    [Header("파츠 (orderIndex 순서대로 등록)")]
    [Tooltip("씬의 PuzzlePart를 orderIndex 오름차순(A, B, C, D)으로 넣어주세요.")]
    public List<PuzzlePart> parts = new();

    [Header("이벤트")]
    public UnityEvent onPuzzleSolved;       // 퍼즐 완성 시 호출
    public UnityEvent onPuzzleReset;        // 파츠 이동/연결 해제 시 호출

    // ── 내부 상태 ───────────────────────────────────────────────────
    bool isSolved = false;

    // ── 외부 호출 API ───────────────────────────────────────────────

    /// <summary>파츠가 드래그되어 연결이 끊길 때 PuzzlePart가 호출</summary>
    public void NotifyPartMoved()
    {
        if (!isSolved) return;
        isSolved = false;
        DevLog.Log("[PuzzleManager] 파츠 이동 감지 — 퍼즐 초기화");
        onPuzzleReset?.Invoke();
    }

    /// <summary>연결이 발생할 때 PuzzlePart가 호출</summary>
    public void NotifyConnected(PuzzlePart movedPart, ConnectorPoint myPt, ConnectorPoint target)
    {
        DevLog.Log($"[PuzzleManager] 연결: {movedPart.partId}({myPt.side}) ↔ " +
                  $"{target.ownerPart.partId}({target.side})");
        ValidateChain();
    }

    // ── 체인 검증 ───────────────────────────────────────────────────

    /// <summary>
    /// parts 리스트의 orderIndex 순서대로
    /// parts[i].Bottom ↔ parts[i+1].Top 이 모두 연결되어 있는지 확인합니다.
    /// </summary>
    public void ValidateChain()
    {
        if (parts == null || parts.Count < 2)
        {
            DevLog.LogWarning("[PuzzleManager] 파츠가 2개 미만입니다. 검증 불가.");
            return;
        }

        // orderIndex 순으로 정렬
        var ordered = parts.OrderBy(p => p.orderIndex).ToList();

        bool chainOk = true;
        var  report  = new System.Text.StringBuilder();
        report.AppendLine("[PuzzleManager] ── 체인 검증 결과 ──");

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            PuzzlePart upper = ordered[i];      // 위쪽 파츠 (Bottom 연결점을 내려보냄)
            PuzzlePart lower = ordered[i + 1];  // 아래쪽 파츠 (Top 연결점을 받음)

            ConnectorPoint upperBottom = GetConnector(upper, ConnectorPoint.Side.Bottom);
            ConnectorPoint lowerTop    = GetConnector(lower, ConnectorPoint.Side.Top);

            bool stepOk = upperBottom != null
                       && lowerTop    != null
                       && upperBottom.IsLinked
                       && upperBottom.linkedTo == lowerTop;

            string mark = stepOk ? "✓" : "✗";
            report.AppendLine($"  {mark} [{upper.partId}].Bottom → [{lower.partId}].Top : {(stepOk ? "연결됨" : "미연결")}");

            if (!stepOk) chainOk = false;
        }

        report.AppendLine(chainOk
            ? $"  ★ 완성! 체인이 올바른 순서({string.Join("→", ordered.Select(p => p.partId))})로 연결되었습니다."
            : "  아직 완성되지 않았습니다.");

        Debug.Log(report.ToString());

        if (chainOk && !isSolved)
        {
            isSolved = true;
            onPuzzleSolved?.Invoke();
        }
    }

    // ── 회전 (UI 버튼 OnClick에 연결) ───────────────────────────────

    /// <summary>현재 마우스가 올라간(Active) 파츠를 90도 회전합니다.</summary>
    public void RotateActivePart()
    {
        if (PuzzlePart.Active != null)
            PuzzlePart.Active.Rotate90();
        else
            DevLog.Log("[PuzzleManager] 회전할 파츠가 선택되지 않았습니다. (파츠 위에 마우스를 올리세요)");
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────

    ConnectorPoint GetConnector(PuzzlePart part, ConnectorPoint.Side side)
    {
        return part.connectors.FirstOrDefault(cp => cp.side == side);
    }

    // ── 에디터용 수동 검증 버튼 ─────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("수동으로 체인 검증 실행")]
    void EditorValidate() => ValidateChain();
#endif
}