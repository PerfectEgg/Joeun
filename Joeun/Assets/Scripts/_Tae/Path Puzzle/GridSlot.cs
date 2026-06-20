using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 격자의 한 칸입니다. Assemble 모드일 때 빈 칸(조립 가능 칸)을 클릭하면
/// 노드가 생성됩니다. 고정 노드(A 등)가 미리 놓인 칸은 assemblable=false.
/// </summary>
public class GridSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("격자 좌표")]
    public int row;
    public int col;

    [Header("설정")]
    public bool isStart;        // A (시작)
    public bool isGoal;         // B (도착)
    public bool assemblable = true;   // 조립 모드에서 노드 생성 가능 여부

    [HideInInspector] public GridNode currentNode;   // 이 칸에 놓인 노드 (없으면 null)

    PathPuzzleManager manager;

    void Awake()
    {
        manager = GetComponentInParent<PathPuzzleManager>();
    }

    public void OnPointerClick(PointerEventData ev)
    {
        if (manager != null && !manager.CanEdit)
            return;

        var mode = PuzzleModeManager.Instance;
        if (mode == null || !mode.IsAssemble) return;     // Assemble 모드 아니면 무시
        if (!assemblable || currentNode != null) return;  // 막힌 칸 / 이미 노드 있음

        manager?.SpawnNodeAt(this);
    }
}
