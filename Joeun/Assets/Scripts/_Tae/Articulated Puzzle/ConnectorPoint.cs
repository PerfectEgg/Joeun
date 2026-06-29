using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파츠의 연결점 하나를 나타냅니다.
/// side 는 '논리적 이름표'로 유지하고(체인 검증용),
/// 실제 연결 가능 여부는 월드 공간에서 서로 마주 보는지로 판단합니다.
/// → 파츠를 회전해도 올바르게 동작합니다.
/// </summary>
public class ConnectorPoint : MonoBehaviour
{
    public enum Side { Top, Bottom }

    [Header("설정")]
    public Side side;                      // 논리적 방향 (체인 검증 식별용)
    public float snapRadius = 40f;         // 스냅 감지 반경 (px)

    [Tooltip("마주 보는 정도 허용치. -1=완전히 반대(정확), -0.7≈45° 여유")]
    public float facingThreshold = -0.7f;

    [Header("비주얼")]
    public Image dotImage;
    public Color idleColor   = new Color(0.6f, 0.6f, 0.6f);
    public Color nearColor   = new Color(0.2f, 0.8f, 0.4f);
    public Color linkedColor = new Color(0.1f, 0.6f, 0.2f);

    [HideInInspector] public PuzzlePart ownerPart;
    [HideInInspector] public ConnectorPoint linkedTo;

    public bool IsLinked => linkedTo != null;

    void Awake()
    {
        ownerPart = GetComponentInParent<PuzzlePart>();
        SetVisual(idleColor);
    }

    /// <summary>
    /// 이 연결점이 월드 공간에서 바라보는 방향.
    /// Top은 파츠의 '위', Bottom은 '아래'. 파츠 회전이 자동 반영됩니다.
    /// </summary>
    public Vector2 WorldFacingDirection()
    {
        Transform t = ownerPart != null ? ownerPart.transform : transform;
        return side == Side.Top ? (Vector2)t.up : (Vector2)(-t.up);
    }

    /// <summary>다른 연결점과 연결 가능한지 — 서로 마주 보고 있어야 함</summary>
    public bool CanConnectTo(ConnectorPoint other)
    {
        if (other == null || other == this) return false;
        if (other.ownerPart == ownerPart)  return false;  // 같은 파츠 불가
        if (IsLinked || other.IsLinked)    return false;  // 이미 연결됨

        // 두 연결점의 facing 방향이 서로 반대일 때만 연결 (마주 봄)
        float dot = Vector2.Dot(WorldFacingDirection().normalized,
                                other.WorldFacingDirection().normalized);
        return dot <= facingThreshold;
    }

    public void LinkTo(ConnectorPoint other)
    {
        linkedTo       = other;
        other.linkedTo = this;
        SetVisual(linkedColor);
        other.SetVisual(linkedColor);
    }

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

    public Vector2 WorldPosition()
    {
        return (transform as RectTransform).position;
    }
}