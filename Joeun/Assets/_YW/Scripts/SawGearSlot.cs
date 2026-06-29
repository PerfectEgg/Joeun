using UnityEngine.Serialization;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SawGearSlot : MonoBehaviour
{
    [SerializeField] private string slotId;
    [FormerlySerializedAs("anchor")]
    [SerializeField] private Transform snapPoint;
    [SerializeField, Min(0.01f)] private float snapRadius = 0.45f;
    [SerializeField] private int snappedSortingOrder;
    [FormerlySerializedAs("requiredSolvedSlot")]
    [SerializeField] private SawGearSlot requiredOccupiedSlot;
    [SerializeField] private bool poweredAtStart;
    [SerializeField] private bool requirePowerToSnap = true;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private int rotationDirection = 1;
    [SerializeField, Min(1)] private int toothCount = 12;
    [SerializeField] private float meshPhaseOffset;
    [SerializeField] private bool waitForToothPhase = true;
    [FormerlySerializedAs("rotationSource")]
    [SerializeField] private Transform rotatingSlotVisual;
    [SerializeField] private bool rotateSlotVisual = true;
    [SerializeField] private bool selfRotateVisualWhenEmpty = true;

    bool runtimePowered;
    bool hasSlotVisualBase;
    Transform cachedSlotVisual;
    Vector3 slotVisualBasePosition;
    Quaternion slotVisualBaseRotation;
    Vector3 snapWorldPosition;
    float currentRotationAngle;
    bool rotationActive;
    float rotationStartDelay;
    bool hasSnapWorldPosition;

    public string SlotId => slotId;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
    public Vector3 SnapWorldPosition => hasSnapWorldPosition ? snapWorldPosition : SnapPoint.position;
    public Transform Anchor => SnapPoint;
    public Transform RotationPivot => SnapPoint;
    public Transform RotatingSlotVisual => rotatingSlotVisual != null ? rotatingSlotVisual : SnapPoint;
    public float CurrentRotationAngle => currentRotationAngle;
    public int ToothCount => Mathf.Max(1, toothCount);
    public float MeshPhaseOffset => meshPhaseOffset;
    public bool WaitForToothPhase => waitForToothPhase;
    public bool IsRotationActive => rotationActive;
    public float SnapRadius => snapRadius;
    public int SnappedSortingOrder => snappedSortingOrder;
    public SawGearPiece Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    public bool HasCorrectOccupant => Occupant != null && IsCorrectPiece(Occupant);
    public bool IsPowered => poweredAtStart || runtimePowered || (requiredOccupiedSlot != null && requiredOccupiedSlot.IsOccupied);
    public float SignedRotationSpeed => rotationSpeed * (rotationDirection < 0 ? -1f : 1f);

    private void OnValidate()
    {
        if (snapPoint == null)
            snapPoint = transform;

        if (rotatingSlotVisual == null)
            rotatingSlotVisual = snapPoint;

        if (rotationDirection == 0)
            rotationDirection = 1;
    }

    private void Awake()
    {
        CacheSnapWorldPosition();
        CacheSlotVisualBase();
    }

    private void OnEnable()
    {
        if (!hasSnapWorldPosition)
            CacheSnapWorldPosition();
    }

    public bool CanReceive(SawGearPiece piece)
    {
        if (piece == null || IsOccupied)
            return false;

        return !requirePowerToSnap || IsPowered;
    }

    public bool IsCorrectPiece(SawGearPiece piece)
    {
        if (piece == null)
            return false;

        return string.Equals(slotId, piece.CorrectSlotId, System.StringComparison.Ordinal);
    }

    public void SetRuntimePowered(bool powered)
    {
        runtimePowered = powered;
    }

    public void SetOccupant(SawGearPiece piece)
    {
        Occupant = piece;
    }

    public void ClearOccupant(SawGearPiece piece)
    {
        if (Occupant == piece)
            Occupant = null;
    }

    public float GetRotationStep(float deltaTime)
    {
        if (!IsPowered)
            return 0f;

        return SignedRotationSpeed * deltaTime;
    }

    public void StartRotation(float delay)
    {
        if (rotationActive || rotationStartDelay > 0f)
            return;

        rotationStartDelay = Mathf.Max(0f, delay);
        if (rotationStartDelay <= 0f)
            rotationActive = true;
    }

    public bool TickRotationStart(float deltaTime)
    {
        if (rotationActive)
            return true;

        if (rotationStartDelay <= 0f)
            return false;

        rotationStartDelay -= deltaTime;
        if (rotationStartDelay > 0f)
            return false;

        rotationActive = true;
        rotationStartDelay = 0f;
        return true;
    }

    public void StopRotation()
    {
        rotationActive = false;
        rotationStartDelay = 0f;
    }

    public void RotateSlotVisualBy(float degrees)
    {
        if (Mathf.Abs(degrees) <= 0.001f)
            return;

        SetCurrentRotationAngle(currentRotationAngle + degrees);
    }

    public void SetCurrentRotationAngle(float angle)
    {
        currentRotationAngle = Mathf.Repeat(angle, 360f);

        if (rotateSlotVisual)
            ApplySlotVisualRotation();
    }

    private void ApplySlotVisualRotation()
    {
        Transform visual = RotatingSlotVisual;
        if (visual == null)
            return;

        CacheSlotVisualBase();
        visual.position = slotVisualBasePosition;
        visual.rotation = slotVisualBaseRotation;

        if (!IsOccupied)
        {
            if (selfRotateVisualWhenEmpty)
                visual.rotation = slotVisualBaseRotation * Quaternion.Euler(0f, 0f, currentRotationAngle);

            return;
        }

        visual.RotateAround(SnapWorldPosition, Vector3.forward, currentRotationAngle);
    }

    private void CacheSlotVisualBase()
    {
        Transform visual = RotatingSlotVisual;
        if (visual == null)
            return;

        if (hasSlotVisualBase && cachedSlotVisual == visual)
            return;

        cachedSlotVisual = visual;
        slotVisualBasePosition = visual.position;
        slotVisualBaseRotation = visual.rotation;
        hasSlotVisualBase = true;
    }

    private void CacheSnapWorldPosition()
    {
        snapWorldPosition = SnapPoint.position;
        hasSnapWorldPosition = true;
    }
}
