using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SawGearPuzzleController : MonoBehaviour, IPickable
{
    [SerializeField] private SawGearPiece[] pieces;
    [SerializeField] private SawGearSlot[] slots;
    [SerializeField] private string startItemId;
    [SerializeField] private bool started = true;
    [SerializeField] private bool lockWhenSolved = true;
    [SerializeField] private GameObject[] visibleBeforeStart;
    [SerializeField] private GameObject[] visibleAfterStart;
    [SerializeField] private GameObject[] visibleAfterSolved;
    [SerializeField] private ConveyorBeltController[] conveyorsToUnlockOnSolved;
    [SerializeField] private bool handlePuzzleDragInput = true;
    [SerializeField] private bool syncMeshedSlotsByToothCount = true;
    [SerializeField, Range(0.1f, 2f)] private float rotationSpeedMultiplier = 0.65f;
    [SerializeField] private Color wrongFeedbackColor = new Color(1f, 0.12f, 0.08f, 1f);
    [SerializeField, Min(0f)] private float wrongFeedbackDuration = 0.35f;
    [SerializeField, Range(1f, 3f)] private float wrongFeedbackDurationMultiplier = 1.6f;
    [SerializeField] private UnityEvent onSolved;

    bool solved;
    SawGearPiece activeDraggedPiece;

    public bool IsLocked => !started && !solved;
    public bool IsSolved => solved;

    private void Reset()
    {
        CollectChildren();
    }

    private void Awake()
    {
        CollectChildrenIfEmpty();
        ApplyStateObjects();
    }

    private void OnEnable()
    {
        ApplyStateObjects();
    }

    private void Update()
    {
        if (started)
            ApplyPoweredRotation();

        if (started && (!solved || !lockWhenSolved))
            HandlePuzzleDragInput();
    }

    private void OnDisable()
    {
        if (activeDraggedPiece == null)
            return;

        activeDraggedPiece.OnDragEnd();
        activeDraggedPiece = null;
    }

    public bool TrySnap(SawGearPiece piece, out SawGearSlot bestSlot)
    {
        CollectChildrenIfEmpty();
        RefreshPowerState();

        bestSlot = null;
        if (piece == null || slots == null)
            return false;

        float bestDistance = float.MaxValue;
        foreach (SawGearSlot slot in slots)
        {
            if (slot == null || !slot.CanReceive(piece))
                continue;

            float distance = Vector2.Distance(piece.SnapPoint.position, slot.SnapWorldPosition);
            if (distance > slot.SnapRadius || distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestSlot = slot;
        }

        return bestSlot != null;
    }

    public void NotifyPieceSnapped(SawGearPiece piece)
    {
        if (piece == null)
            return;

        ApplyPoweredRotation();

        if (!IsValidSequenceAfter(piece))
        {
            RejectPlacement(piece);
            return;
        }

        if (solved || !AreSlotsSolved())
            return;

        solved = true;
        started = true;
        ApplyStateObjects();
        MarkLinkedConveyorsSolved();
        onSolved?.Invoke();
    }

    public void NotifyPieceReleased(SawGearPiece piece)
    {
        if (lockWhenSolved)
            return;

        if (!solved)
            return;

        solved = false;
        ApplyStateObjects();
    }

    public void ResetPuzzle()
    {
        CollectChildrenIfEmpty();

        if (pieces != null)
        {
            foreach (SawGearPiece piece in pieces)
            {
                if (piece != null)
                    piece.ResetToInitial();
            }
        }

        solved = false;
        ApplyStateObjects();
    }

    public bool TryUnlock(string keyId)
    {
        if (solved || started)
            return false;

        if (!string.IsNullOrEmpty(startItemId) &&
            !string.Equals(keyId, startItemId, System.StringComparison.Ordinal))
        {
            return false;
        }

        StartPuzzle();
        return true;
    }

    public void StartPuzzle()
    {
        if (solved)
            return;

        started = true;
        ApplyStateObjects();
    }

    public void SetPuzzleStarted(bool value)
    {
        if (solved && !value)
            return;

        started = value;
        if (!started)
            StopAllSlots();

        ApplyStateObjects();
    }

    public bool CanPlaceInSlot(SawGearPiece piece, SawGearSlot slot)
    {
        CollectChildrenIfEmpty();

        if (piece == null || slot == null)
            return false;

        int placedIndex = IndexOfSlot(slot);
        if (placedIndex < 0)
            return false;

        if (placedIndex == 0)
            return true;

        for (int i = 0; i < placedIndex; i++)
        {
            SawGearSlot previousSlot = slots[i];
            if (previousSlot == null || !previousSlot.HasCorrectOccupant)
                return false;
        }

        return slot.IsCorrectPiece(piece);
    }

    public void RejectPlacement(SawGearPiece piece)
    {
        if (piece == null)
            return;

        piece.ClearCurrentSlot();
        piece.PlayWrongFeedback(wrongFeedbackColor, wrongFeedbackDuration * wrongFeedbackDurationMultiplier);
    }

    public bool CanPieceStartDrag(SawGearPiece piece)
    {
        if (!started || (solved && lockWhenSolved))
            return false;

        if (!handlePuzzleDragInput || piece == null)
            return true;

        if (activeDraggedPiece == piece)
            return true;

        Camera camera = Camera.main;
        return camera == null || FindTopPieceUnderMouse(camera) == piece;
    }

    private bool AreSlotsSolved()
    {
        CollectChildrenIfEmpty();

        if (slots == null || slots.Length == 0)
            return false;

        foreach (SawGearSlot slot in slots)
        {
            if (slot == null || !slot.HasCorrectOccupant)
                return false;
        }

        return true;
    }

    private bool IsValidSequenceAfter(SawGearPiece placedPiece)
    {
        CollectChildrenIfEmpty();

        if (placedPiece == null || placedPiece.CurrentSlot == null)
            return false;

        int placedIndex = IndexOfSlot(placedPiece.CurrentSlot);
        if (placedIndex < 0)
            return false;

        if (placedIndex == 0)
            return true;

        for (int i = 0; i <= placedIndex; i++)
        {
            SawGearSlot slot = slots[i];
            if (slot == null || !slot.HasCorrectOccupant)
                return false;
        }

        return true;
    }

    private int IndexOfSlot(SawGearSlot target)
    {
        if (target == null || slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == target)
                return i;
        }

        return -1;
    }

    private void ApplyPoweredRotation()
    {
        CollectChildrenIfEmpty();
        RefreshPowerState();

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                SawGearSlot slot = slots[i];
                if (slot == null)
                    continue;

                if (!slot.IsPowered)
                {
                    slot.StopRotation();
                    ApplyOccupantRotation(slot);
                    continue;
                }

                SawGearSlot driveSlot = syncMeshedSlotsByToothCount && i > 0 ? slots[i - 1] : null;
                if (driveSlot != null && driveSlot.IsRotationActive)
                {
                    StartDrivenSlotWhenReady(slot, driveSlot);
                    if (slot.TickRotationStart(Time.deltaTime))
                        SyncDrivenSlotAngle(slot, driveSlot);
                }
                else
                {
                    slot.StartRotation(0f);
                    if (slot.TickRotationStart(Time.deltaTime))
                        slot.RotateSlotVisualBy(slot.GetRotationStep(Time.deltaTime) * rotationSpeedMultiplier);
                }

                ApplyOccupantRotation(slot);
            }
        }

        if (pieces == null)
            return;

        foreach (SawGearPiece piece in pieces)
        {
            if (piece != null && !piece.IsSnapped)
                piece.SetRotationSpeed(0f);
        }
    }

    private void ApplyOccupantRotation(SawGearSlot slot)
    {
        if (slot == null || slot.Occupant == null)
            return;

        slot.Occupant.SetRotationSpeed(0f);
        if (slot.IsPowered && slot.IsRotationActive)
            slot.Occupant.FollowRotation(slot.CurrentRotationAngle, slot.SnapWorldPosition);
    }

    private void StartDrivenSlotWhenReady(SawGearSlot slot, SawGearSlot driveSlot)
    {
        if (slot == null || driveSlot == null || slot.IsRotationActive)
            return;

        float delay = slot.WaitForToothPhase ? CalculateNextToothPhaseDelay(driveSlot) : 0f;
        slot.StartRotation(delay);
    }

    private void SyncDrivenSlotAngle(SawGearSlot slot, SawGearSlot driveSlot)
    {
        float ratio = driveSlot.ToothCount / (float)slot.ToothCount;
        float angle = slot.MeshPhaseOffset - driveSlot.CurrentRotationAngle * ratio;
        slot.SetCurrentRotationAngle(angle);
    }

    private float CalculateNextToothPhaseDelay(SawGearSlot driveSlot)
    {
        if (driveSlot == null)
            return 0f;

        float speed = Mathf.Abs(driveSlot.SignedRotationSpeed * rotationSpeedMultiplier);
        if (speed <= 0.001f)
            return 0f;

        float toothPitch = 360f / driveSlot.ToothCount;
        float phase = Mathf.Repeat(driveSlot.CurrentRotationAngle, toothPitch);
        if (phase <= 0.01f || toothPitch - phase <= 0.01f)
            return 0f;

        return (toothPitch - phase) / speed;
    }

    private void RefreshPowerState()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            bool poweredBySequence = i == 0 || (slots[i - 1] != null && slots[i - 1].IsOccupied);
            slots[i].SetRuntimePowered(poweredBySequence);
        }
    }

    private void CollectChildrenIfEmpty()
    {
        if (pieces == null || pieces.Length == 0 || slots == null || slots.Length == 0)
            CollectChildren();
    }

    private void CollectChildren()
    {
        pieces = GetComponentsInChildren<SawGearPiece>(true);
        slots = GetComponentsInChildren<SawGearSlot>(true);
    }

    private void StopAllSlots()
    {
        CollectChildrenIfEmpty();

        if (slots == null)
            return;

        foreach (SawGearSlot slot in slots)
        {
            if (slot != null)
                slot.StopRotation();
        }
    }

    private void ApplyStateObjects()
    {
        SetObjectsActive(visibleBeforeStart, !started && !solved);
        SetObjectsActive(visibleAfterStart, started || solved);
        SetObjectsActive(visibleAfterSolved, solved);
    }

    private void MarkLinkedConveyorsSolved()
    {
        if (conveyorsToUnlockOnSolved == null)
            return;

        foreach (ConveyorBeltController conveyor in conveyorsToUnlockOnSolved)
        {
            if (conveyor != null)
                conveyor.MarkSawPuzzleSolved();
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }
    }

    private void HandlePuzzleDragInput()
    {
        if (!handlePuzzleDragInput)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        if (Input.GetMouseButtonUp(0) && activeDraggedPiece != null)
        {
            activeDraggedPiece.OnDragEnd();
            activeDraggedPiece = null;
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && activeDraggedPiece == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            activeDraggedPiece = FindTopPieceUnderMouse(camera);
            activeDraggedPiece?.OnDragStart();
        }
        else if (Input.GetMouseButton(0) && activeDraggedPiece != null)
        {
            activeDraggedPiece.OnDrag(camera.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    private SawGearPiece FindTopPieceUnderMouse(Camera camera)
    {
        CollectChildrenIfEmpty();

        Vector2 mousePosition = camera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        SawGearPiece bestPiece = null;
        int bestSortingOrder = int.MinValue;
        float bestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            SawGearPiece piece = hit.GetComponentInParent<SawGearPiece>();
            if (piece == null || !piece.CanStartDrag || !ContainsPiece(piece))
                continue;

            int sortingOrder = piece.PickSortingOrder;
            float distance = Vector2.SqrMagnitude((Vector2)piece.SnapPoint.position - mousePosition);
            if (sortingOrder < bestSortingOrder)
                continue;

            if (sortingOrder == bestSortingOrder && distance >= bestDistance)
                continue;

            bestPiece = piece;
            bestSortingOrder = sortingOrder;
            bestDistance = distance;
        }

        return bestPiece;
    }

    private bool ContainsPiece(SawGearPiece piece)
    {
        if (piece == null)
            return false;

        if (pieces == null || pieces.Length == 0)
            return true;

        foreach (SawGearPiece candidate in pieces)
        {
            if (candidate == piece)
                return true;
        }

        return false;
    }
}
