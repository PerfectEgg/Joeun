using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 격자 위의 방향 노드입니다. 화살표가 가리키는 방향으로 경로가 진행됩니다.
/// Rotate 모드일 때 클릭하면 90도씩 방향이 회전합니다.
/// </summary>
public class GridNode : MonoBehaviour, IPointerClickHandler
{
    public enum Dir { Right, Down, Left, Up }

    [Header("방향")]
    public Dir direction = Dir.Right;

    [Header("참조")]
    public RectTransform arrow;       // 회전시킬 화살표 이미지 (없으면 자기 자신)
    public Image background;          // 밝아짐 표시용 배경

    [Header("색상")]
    public Color idleColor = new Color(0.15f, 0.15f, 0.18f);
    public Color litColor  = new Color(0.35f, 0.75f, 0.95f);

    // 격자 좌표 (PathPuzzleManager가 설정)
    [HideInInspector] public int row, col;

    void Start()
    {
        ApplyRotation();
        SetLit(false);
    }

    public void OnPointerClick(PointerEventData ev)
    {
        if (PuzzleModeManager.Instance != null && PuzzleModeManager.Instance.IsRotate)
            Rotate();
    }

    /// <summary>방향을 시계방향으로 90도 회전</summary>
    public void Rotate()
    {
        direction = (Dir)(((int)direction + 1) % 4);
        ApplyRotation();
    }

    void ApplyRotation()
    {
        var target = arrow != null ? arrow : (RectTransform)transform;
        target.localEulerAngles = new Vector3(0f, 0f, ZAngle(direction));
    }

    public void SetLit(bool on)
    {
        if (background != null) background.color = on ? litColor : idleColor;
    }

    // ── 방향 → 시각 각도 / 격자 이동량 ─────────────────────────────
    // 화살표 스프라이트 기본이 '오른쪽(>)' 을 향한다고 가정합니다.

    public static float ZAngle(Dir d)
    {
        switch (d)
        {
            case Dir.Right: return 0f;
            case Dir.Up:    return 90f;
            case Dir.Left:  return 180f;
            case Dir.Down:  return 270f;
            default:        return 0f;
        }
    }

    /// <summary>(row 증가량, col 증가량). Down은 row+1 (화면상 아래).</summary>
    public static Vector2Int Delta(Dir d)
    {
        switch (d)
        {
            case Dir.Right: return new Vector2Int(0, 1);
            case Dir.Down:  return new Vector2Int(1, 0);
            case Dir.Left:  return new Vector2Int(0, -1);
            case Dir.Up:    return new Vector2Int(-1, 0);
            default:        return Vector2Int.zero;
        }
    }
}
