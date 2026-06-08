using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파츠의 연결점 하나를 나타냅니다.
/// 연결 가능한 방향(Top/Bottom)과 현재 연결 상태를 관리합니다.
/// </summary>
public class ConnectorPoint : MonoBehaviour
{
    public enum Side { Top, Bottom }

    [Header("설정")]
    public Side side;                      // 이 연결점의 방향
    public float snapRadius = 40f;         // 스냅 감지 반경 (px)

    [Header("비주얼")]
    public Image dotImage;                 // 연결점 원형 이미지
    public Color idleColor   = new Color(0.6f, 0.6f, 0.6f);   // 비연결 상태
    public Color nearColor   = new Color(0.2f, 0.8f, 0.4f);   // 근접 강조
    public Color linkedColor = new Color(0.1f, 0.6f, 0.2f);   // 연결 완료

    [HideInInspector] public PuzzlePart ownerPart;     // 이 연결점을 소유한 파츠
    [HideInInspector] public ConnectorPoint linkedTo;  // 현재 연결된 상대 연결점

    public bool IsLinked => linkedTo != null;

    void Awake()
    {
        ownerPart = GetComponentInParent<PuzzlePart>();
        SetVisual(idleColor);
    }

    /// <summary>다른 연결점과 연결이 가능한지 검사합니다 (방향이 반대여야 함)</summary>
    public bool CanConnectTo(ConnectorPoint other)
    {
        if (other == null || other == this) return false;
        if (other.ownerPart == ownerPart)  return false;  // 같은 파츠끼리 불가
        if (IsLinked || other.IsLinked)    return false;  // 이미 연결된 점 불가
        // Top ↔ Bottom 만 연결 가능
        return (side == Side.Bottom && other.side == Side.Top)
            || (side == Side.Top   && other.side == Side.Bottom);
    }

    /// <summary>두 연결점을 서로 연결합니다</summary>
    public void LinkTo(ConnectorPoint other)
    {
        linkedTo       = other;
        other.linkedTo = this;
        SetVisual(linkedColor);
        other.SetVisual(linkedColor);
    }

    /// <summary>연결을 해제합니다</summary>
    public void Unlink()
    {
        if (linkedTo != null)
        {
            linkedTo.linkedTo = null;
            linkedTo.SetVisual(idleColor);
            linkedTo = null;
        }
        SetVisual(idleColor);
    }

    public void SetVisual(Color c)
    {
        if (dotImage != null) dotImage.color = c;
    }

    /// <summary>월드(캔버스) 좌표를 반환합니다</summary>
    public Vector2 WorldPosition()
    {
        return (transform as RectTransform).position;
    }
}
