using System;
using UnityEngine;

#pragma warning disable CS0649

[DisallowMultipleComponent]
public sealed class HandPuzzleSlot : MonoBehaviour
{
    [Serializable]
    private struct NeighborLink
    {
        public HandPuzzleDirection direction;
        public HandPuzzleSlot slot;
    }

    [Serializable]
    private struct DirectionAnchor
    {
        public HandPuzzleDirection direction;
        public Transform anchor;
    }

    [Serializable]
    private struct BaseConnectionRule
    {
        public HandPuzzleSlot sourceSlot;
        public HandPuzzleDirectionMask requiredPieceConnections;
        public Transform fromAnchor;
        public Transform toAnchor;
    }

    [SerializeField] private string slotId;
    [SerializeField] private string correctPieceId;
    [SerializeField] private Transform snapPoint;
    [SerializeField, Min(0.01f)] private float snapRadius = 0.45f;
    [SerializeField] private int snappedSortingOrder = 10;
    [SerializeField] private bool requiredForSolved = true;
    [SerializeField] private bool allowSnap = true;
    [SerializeField] private bool fixedConnectionNode;
    [SerializeField] private HandPuzzleDirectionMask allowedConnections = HandPuzzleDirectionMask.All;
    [SerializeField] private NeighborLink[] neighbors = Array.Empty<NeighborLink>();
    [SerializeField] private DirectionAnchor[] connectionAnchors = Array.Empty<DirectionAnchor>();
    [SerializeField] private BaseConnectionRule[] baseConnectionRules = Array.Empty<BaseConnectionRule>();

    SpriteRenderer[] feedbackRenderers;
    Color[] feedbackBaseColors;

    public string SlotId => slotId;
    public string CorrectPieceId => correctPieceId;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
    public Vector3 SnapWorldPosition => SnapPoint.position;
    public float SnapRadius => snapRadius;
    public int SnappedSortingOrder => snappedSortingOrder;
    public bool RequiredForSolved => requiredForSolved;
    public HandPuzzlePiece Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    public bool IsFixedConnectionNode => fixedConnectionNode;
    public bool IsConnectionActive => fixedConnectionNode || Occupant != null;
    public bool HasCorrectOccupant => !requiredForSolved || IsCorrectPiece(Occupant);

    private void OnValidate()
    {
        if (snapPoint == null)
            snapPoint = transform;
    }

    public bool CanReceive(HandPuzzlePiece piece)
    {
        return CanReceive(piece, false);
    }

    public bool CanReceive(HandPuzzlePiece piece, bool allowReplace)
    {
        if (!allowSnap || piece == null)
            return false;

        return Occupant == null || Occupant == piece || allowReplace;
    }

    public bool IsCorrectPiece(HandPuzzlePiece piece)
    {
        if (!requiredForSolved)
            return true;

        if (piece == null || string.IsNullOrEmpty(correctPieceId))
            return false;

        return string.Equals(piece.PieceId, correctPieceId, StringComparison.Ordinal);
    }

    public void SetOccupant(HandPuzzlePiece piece)
    {
        Occupant = piece;
    }

    public void ClearOccupant(HandPuzzlePiece piece)
    {
        if (Occupant == piece)
            Occupant = null;
    }

    public bool AllowsConnection(HandPuzzleDirection direction)
    {
        return allowedConnections.Contains(direction);
    }

    public bool HasConnectionOpening(HandPuzzleDirection direction)
    {
        if (!AllowsConnection(direction))
            return false;

        if (fixedConnectionNode)
            return true;

        return Occupant != null && Occupant.AllowsConnection(direction);
    }

    public bool HasPieceConnectionOpening(HandPuzzleDirection direction, bool requireSlotOpening)
    {
        if (requireSlotOpening && !AllowsConnection(direction))
            return false;

        return Occupant != null && Occupant.AllowsConnection(direction);
    }

    public HandPuzzleSlot GetNeighbor(HandPuzzleDirection direction)
    {
        if (neighbors == null)
            return null;

        foreach (NeighborLink link in neighbors)
        {
            if (link.direction == direction)
                return link.slot;
        }

        return null;
    }

    public Vector3 GetConnectionWorldPosition(HandPuzzleDirection direction)
    {
        Transform anchor = GetConnectionAnchor(direction);
        return anchor != null ? anchor.position : SnapWorldPosition;
    }

    public int BaseConnectionRuleCount => baseConnectionRules != null ? baseConnectionRules.Length : 0;

    public bool TryGetBaseConnectionLine(int index, out Vector3 from, out Vector3 to)
    {
        from = SnapWorldPosition;
        to = SnapWorldPosition;

        if (baseConnectionRules == null || index < 0 || index >= baseConnectionRules.Length)
            return false;

        BaseConnectionRule rule = baseConnectionRules[index];
        HandPuzzleSlot sourceSlot = rule.sourceSlot != null ? rule.sourceSlot : this;
        HandPuzzlePiece sourcePiece = sourceSlot.Occupant;

        if (rule.toAnchor == null || sourcePiece == null)
            return false;

        if (!sourcePiece.HasAllConnections(rule.requiredPieceConnections))
            return false;

        from = rule.fromAnchor != null ? rule.fromAnchor.position : SnapWorldPosition;
        to = rule.toAnchor.position;
        return true;
    }

    public void ShowWrongFeedback(Color color)
    {
        CacheFeedbackRenderers();

        if (feedbackRenderers == null)
            return;

        for (int i = 0; i < feedbackRenderers.Length; i++)
        {
            if (feedbackRenderers[i] != null)
                feedbackRenderers[i].color = color;
        }
    }

    public void ClearWrongFeedback()
    {
        if (feedbackRenderers == null || feedbackBaseColors == null)
            return;

        for (int i = 0; i < feedbackRenderers.Length && i < feedbackBaseColors.Length; i++)
        {
            if (feedbackRenderers[i] != null)
                feedbackRenderers[i].color = feedbackBaseColors[i];
        }
    }

    private void CacheFeedbackRenderers()
    {
        if (feedbackRenderers != null)
            return;

        feedbackRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        feedbackBaseColors = new Color[feedbackRenderers.Length];

        for (int i = 0; i < feedbackRenderers.Length; i++)
            feedbackBaseColors[i] = feedbackRenderers[i] != null ? feedbackRenderers[i].color : Color.white;
    }

    private Transform GetConnectionAnchor(HandPuzzleDirection direction)
    {
        if (connectionAnchors == null)
            return null;

        foreach (DirectionAnchor entry in connectionAnchors)
        {
            if (entry.direction == direction)
                return entry.anchor;
        }

        return null;
    }
}

#pragma warning restore CS0649
