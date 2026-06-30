using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class HandPuzzleController : MonoBehaviour, IPuzzleable
{
    const string AutoSolvedItemObjectName = "Hand Item";

    [Header("Parts")]
    [SerializeField] private HandPuzzlePiece[] pieces;
    [SerializeField] private HandPuzzleSlot[] slots;

    [Header("Rules")]
    [SerializeField] private bool started = true;
    [SerializeField] private bool lockWhenSolved = true;
    [SerializeField] private bool replaceOccupiedSlots = true;
    [SerializeField] private bool handlePuzzleDragInput = true;
    [SerializeField] private bool autoCollectChildren = true;
    [SerializeField] private bool returnPiecesToStartOnMiss;

    [Header("Assemble Submit")]
    [SerializeField] private bool requireAssembleSubmit = true;
    [SerializeField] private Collider2D assembleClickArea;
    [SerializeField] private GameObject assembleReadyHighlight;
    [SerializeField] private bool highlightOnlyInAssembleMode = true;
    [SerializeField] private Color wrongSlotColor = new Color(1f, 0.05f, 0.05f, 0.65f);
    [SerializeField, Min(0f)] private float wrongFeedbackDuration = 0.45f;
    [SerializeField] private bool resetWrongPiecesOnFail = true;

    [Header("Solved Item")]
    [SerializeField] private bool disablePieceCollidersOnSolved = true;
    [SerializeField] private bool disableSolvedItemHooksUntilSolved = true;
    [SerializeField] private Behaviour[] enableComponentsOnSolved;
    [SerializeField] private Collider2D[] enableCollidersOnSolved;
    [SerializeField] private GameObject[] activateObjectsOnSolved;

    [Header("Connection Visual")]
    [SerializeField] private Sprite connectionSprite;
    [SerializeField] private Transform connectionRoot;
    [SerializeField] private Color connectionColor = Color.white;
    [SerializeField] private int connectionSortingOrder = 20;
    [SerializeField, Min(0.001f)] private float connectionThickness = 0.08f;
    [SerializeField] private float connectionRotationOffset;
    [SerializeField] private float connectionZOffset = -0.01f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved;

    readonly List<GameObject> connectionObjects = new List<GameObject>();
    bool solved;
    bool assembleReady;
    bool isSubmitting;
    bool hasAutoSelectedAssembleForReady;
    HandPuzzlePiece activeDraggedPiece;
    Coroutine submitRoutine;
    SpriteRendererAssembleOutlineTarget assembleOutlineTarget;

    public bool IsSolved => solved;
    public bool IsReadyForAssemble => assembleReady && !solved && !isSubmitting;

    private void Reset()
    {
        CollectChildren();
    }

    private void Awake()
    {
        CollectChildrenIfEmpty();
        ResolveAssembleOutlineTarget();
        ApplySolvedItemHooks(false);
        RefreshConnections();
        RefreshAssembleReady();
    }

    private void OnEnable()
    {
        ResolveAssembleOutlineTarget();
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        RefreshAssembleReady();
    }

    private void Update()
    {
        if (started && (!solved || !lockWhenSolved))
        {
            if (HandleAssembleSubmitInput())
                return;

            HandlePuzzleDragInput();
        }
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        StopSubmitRoutine();

        if (activeDraggedPiece == null)
            return;

        activeDraggedPiece.OnDragEnd();
        activeDraggedPiece = null;
    }

    public void StartPuzzle()
    {
        if (solved)
            return;

        started = true;
    }

    public void CompletePuzzle()
    {
        if (solved)
            return;

        solved = true;
        RefreshAssembleReady();
        ClearConnections();
        ApplySolvedItemHooks(true);

        if (disablePieceCollidersOnSolved)
            SetPieceCollidersEnabled(false);

        GameEvent.ESFXPlay?.Invoke("Puzzle_Success");
        onSolved?.Invoke();
    }

    public bool TrySnap(HandPuzzlePiece piece, out HandPuzzleSlot bestSlot)
    {
        CollectChildrenIfEmpty();

        bestSlot = null;
        if (piece == null || slots == null)
            return false;

        float bestDistance = float.MaxValue;
        foreach (HandPuzzleSlot slot in slots)
        {
            if (slot == null)
                continue;

            if (!slot.CanReceive(piece, replaceOccupiedSlots))
                continue;

            float distance = Vector2.Distance(piece.SnapPoint.position, slot.SnapWorldPosition);
            if (distance > slot.SnapRadius || distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestSlot = slot;
        }

        return bestSlot != null;
    }

    public void NotifyPieceSnapped(HandPuzzlePiece piece)
    {
        if (piece == null || piece.CurrentSlot == null)
            return;

        GameEvent.ESFXPlay?.Invoke("Hand_Input");

        if (replaceOccupiedSlots)
            ClearReplacedOccupant(piece.CurrentSlot, piece);

        RefreshConnections();
        if (requireAssembleSubmit)
            RefreshAssembleReady();
        else
            CheckSolved();
    }

    public void NotifyPieceReleased(HandPuzzlePiece piece)
    {
        RefreshConnections();
        RefreshAssembleReady();

        if (!solved)
            return;

        solved = false;
    }

    public bool CanPieceStartDrag(HandPuzzlePiece piece)
    {
        if (!started || isSubmitting || (solved && lockWhenSolved))
            return false;

        if (!handlePuzzleDragInput || piece == null)
            return true;

        if (activeDraggedPiece == piece)
            return true;

        Camera camera = Camera.main;
        return camera == null || FindTopPieceUnderMouse(camera) == piece;
    }

    public bool ShouldReturnPieceToStartOnMiss(HandPuzzlePiece piece)
    {
        return returnPiecesToStartOnMiss && piece != null && piece.ReturnToStartOnMiss;
    }

    public void SubmitAssembly()
    {
        if (solved || isSubmitting)
            return;

        CollectChildrenIfEmpty();

        if (!AreRequiredSlotsFilled())
        {
            RefreshAssembleReady();
            return;
        }

        List<HandPuzzleSlot> wrongSlots = GetWrongRequiredSlots();
        if (wrongSlots.Count == 0)
        {
            CompletePuzzle();
            return;
        }

        if (submitRoutine != null)
            StopCoroutine(submitRoutine);

        submitRoutine = StartCoroutine(WrongSubmitRoutine(wrongSlots));
    }

    public void ResetPuzzle()
    {
        CollectChildrenIfEmpty();
        StopSubmitRoutine();
        SetPieceCollidersEnabled(true);
        ApplySolvedItemHooks(false);

        if (pieces != null)
        {
            foreach (HandPuzzlePiece piece in pieces)
            {
                if (piece != null)
                    piece.ResetToInitial();
            }
        }

        solved = false;
        RefreshAssembleReady();
        RefreshConnections();
    }

    private void CheckSolved()
    {
        if (solved || !AreSlotsSolved())
            return;

        CompletePuzzle();
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        RefreshAssembleReady();
    }

    private bool HandleAssembleSubmitInput()
    {
        if (!requireAssembleSubmit || !assembleReady || isSubmitting || solved)
            return false;

        if (SkillIconModeView.CurrentMode != SkillModeType.Assemble)
            return false;

        if (!Input.GetMouseButtonDown(0))
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (!IsPointerInsideAssembleArea(Input.mousePosition))
            return false;

        SubmitAssembly();
        return true;
    }

    private bool IsPointerInsideAssembleArea(Vector2 screenPoint)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        if (assembleClickArea != null)
        {
            Vector2 mousePosition = camera.ScreenToWorldPoint(screenPoint);
            return assembleClickArea.OverlapPoint(mousePosition);
        }

        ResolveAssembleOutlineTarget();
        return assembleOutlineTarget != null && assembleOutlineTarget.ContainsScreenPoint(screenPoint);
    }

    private IEnumerator WrongSubmitRoutine(List<HandPuzzleSlot> wrongSlots)
    {
        isSubmitting = true;
        RefreshAssembleReady();

        foreach (HandPuzzleSlot slot in wrongSlots)
            ShowWrongFeedback(slot);

        if (wrongFeedbackDuration > 0f)
            yield return new WaitForSeconds(wrongFeedbackDuration);

        foreach (HandPuzzleSlot slot in wrongSlots)
            ClearWrongFeedback(slot);

        if (resetWrongPiecesOnFail)
        {
            foreach (HandPuzzleSlot slot in wrongSlots)
            {
                HandPuzzlePiece piece = slot != null ? slot.Occupant : null;
                if (piece != null)
                    piece.ResetToInitial();
            }
        }

        GameEvent.ESFXPlay?.Invoke("Puzzle_Erorr");

        isSubmitting = false;
        submitRoutine = null;
        RefreshConnections();
        RefreshAssembleReady();
    }

    private void StopSubmitRoutine()
    {
        if (submitRoutine == null)
            return;

        StopCoroutine(submitRoutine);
        submitRoutine = null;
        isSubmitting = false;

        if (slots == null)
            return;

        foreach (HandPuzzleSlot slot in slots)
            ClearWrongFeedback(slot);
    }

    private void ShowWrongFeedback(HandPuzzleSlot slot)
    {
        HandPuzzlePiece piece = slot != null ? slot.Occupant : null;
        if (piece != null)
        {
            piece.ShowWrongFeedback(wrongSlotColor);
            return;
        }

        slot?.ShowWrongFeedback(wrongSlotColor);
    }

    private void ClearWrongFeedback(HandPuzzleSlot slot)
    {
        HandPuzzlePiece piece = slot != null ? slot.Occupant : null;
        if (piece != null)
        {
            piece.ClearWrongFeedback();
            return;
        }

        slot?.ClearWrongFeedback();
    }

    private bool AreSlotsSolved()
    {
        CollectChildrenIfEmpty();

        if (slots == null || slots.Length == 0)
            return false;

        foreach (HandPuzzleSlot slot in slots)
        {
            if (slot == null || !slot.RequiredForSolved)
                continue;

            if (!slot.HasCorrectOccupant)
                return false;
        }

        return true;
    }

    private bool AreRequiredSlotsFilled()
    {
        CollectChildrenIfEmpty();

        bool hasRequiredSlot = false;
        if (slots == null)
            return false;

        foreach (HandPuzzleSlot slot in slots)
        {
            if (slot == null || !slot.RequiredForSolved)
                continue;

            hasRequiredSlot = true;
            if (!slot.IsOccupied)
                return false;
        }

        return hasRequiredSlot;
    }

    private List<HandPuzzleSlot> GetWrongRequiredSlots()
    {
        List<HandPuzzleSlot> wrongSlots = new List<HandPuzzleSlot>();

        if (slots == null)
            return wrongSlots;

        foreach (HandPuzzleSlot slot in slots)
        {
            if (slot == null || !slot.RequiredForSolved)
                continue;

            if (!slot.HasCorrectOccupant)
                wrongSlots.Add(slot);
        }

        return wrongSlots;
    }

    private void RefreshAssembleReady()
    {
        assembleReady = requireAssembleSubmit && !solved && !isSubmitting && AreRequiredSlotsFilled();

        if (!assembleReady)
            hasAutoSelectedAssembleForReady = false;

        ResolveAssembleOutlineTarget();
        if (assembleReady && assembleOutlineTarget != null)
            assembleOutlineTarget.PrepareOutline();

        if (assembleReady && !hasAutoSelectedAssembleForReady)
        {
            hasAutoSelectedAssembleForReady = true;
            SkillIconModeView.SelectMode(SkillModeType.Assemble);
        }

        if (assembleReadyHighlight == null)
            return;

        bool shouldShow = assembleReady;
        if (highlightOnlyInAssembleMode)
            shouldShow = shouldShow && SkillIconModeView.CurrentMode == SkillModeType.Assemble;

        assembleReadyHighlight.SetActive(shouldShow);
    }

    private void ClearReplacedOccupant(HandPuzzleSlot slot, HandPuzzlePiece placedPiece)
    {
        if (slot == null || placedPiece == null)
            return;

        foreach (HandPuzzlePiece piece in pieces)
        {
            if (piece == null || piece == placedPiece || piece.CurrentSlot != slot)
                continue;

            piece.ResetToInitial();
        }

        slot.SetOccupant(placedPiece);
    }

    private void RefreshConnections()
    {
        ClearConnections();

        if (connectionSprite == null || slots == null)
            return;

        HashSet<string> drawn = new HashSet<string>();
        foreach (HandPuzzleSlot slot in slots)
        {
            if (slot == null || !slot.IsConnectionActive)
                continue;

            for (int i = 0; i < 6; i++)
            {
                HandPuzzleDirection direction = (HandPuzzleDirection)i;
                HandPuzzleSlot neighbor = slot.GetNeighbor(direction);
                if (neighbor == null || !neighbor.IsConnectionActive)
                    continue;

                string key = GetConnectionKey(slot, neighbor);
                if (!drawn.Add(key))
                    continue;

                HandPuzzleDirection opposite = HandPuzzleDirectionUtility.Opposite(direction);
                if (!CanConnect(slot, direction, neighbor, opposite))
                    continue;

                CreateConnection(
                    slot.GetConnectionWorldPosition(direction),
                    neighbor.GetConnectionWorldPosition(opposite));
            }
        }

        CreateBaseConnectionLines();
    }

    private bool CanConnect(
        HandPuzzleSlot a,
        HandPuzzleDirection aDirection,
        HandPuzzleSlot b,
        HandPuzzleDirection bDirection)
    {
        if (a.IsFixedConnectionNode && b.IsFixedConnectionNode)
            return a.HasConnectionOpening(aDirection) && b.HasConnectionOpening(bDirection);

        if (a.IsFixedConnectionNode)
            return a.HasConnectionOpening(aDirection)
                && b.HasPieceConnectionOpening(bDirection, false);

        if (b.IsFixedConnectionNode)
            return a.HasPieceConnectionOpening(aDirection, false)
                && b.HasConnectionOpening(bDirection);

        return a.HasConnectionOpening(aDirection)
            && b.HasConnectionOpening(bDirection);
    }

    private void CreateConnection(Vector3 a, Vector3 b)
    {
        Transform parent = connectionRoot != null ? connectionRoot : transform;
        GameObject line = new GameObject("__HandPuzzleConnection");
        line.transform.SetParent(parent, true);

        SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
        renderer.sprite = connectionSprite;
        renderer.color = connectionColor;
        renderer.sortingOrder = connectionSortingOrder;

        Vector3 delta = b - a;
        float length = delta.magnitude;
        Vector3 mid = (a + b) * 0.5f;
        mid.z += connectionZOffset;

        line.transform.position = mid;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + connectionRotationOffset;
        line.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector2 spriteSize = connectionSprite.bounds.size;
        float xScale = spriteSize.x > 0.001f ? length / spriteSize.x : 1f;
        float yScale = spriteSize.y > 0.001f ? connectionThickness / spriteSize.y : 1f;
        line.transform.localScale = new Vector3(xScale, yScale, 1f);

        connectionObjects.Add(line);
    }

    private void ClearConnections()
    {
        foreach (GameObject obj in connectionObjects)
        {
            if (obj == null)
                continue;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        connectionObjects.Clear();
    }

    private string GetConnectionKey(HandPuzzleSlot a, HandPuzzleSlot b)
    {
        int aId = a.GetInstanceID();
        int bId = b.GetInstanceID();
        return aId < bId ? $"{aId}:{bId}" : $"{bId}:{aId}";
    }

    private void CreateBaseConnectionLines()
    {
        if (slots == null)
            return;

        foreach (HandPuzzleSlot slot in slots)
        {
            if (slot == null)
                continue;

            for (int i = 0; i < slot.BaseConnectionRuleCount; i++)
            {
                if (slot.TryGetBaseConnectionLine(i, out Vector3 from, out Vector3 to))
                    CreateConnection(from, to);
            }
        }
    }

    private void ApplySolvedItemHooks(bool value)
    {
        if (!value && !disableSolvedItemHooksUntilSolved)
            return;

        if (enableComponentsOnSolved != null)
        {
            foreach (Behaviour component in enableComponentsOnSolved)
            {
                if (component == null || component == this)
                    continue;

                component.enabled = value;
            }
        }

        if (enableCollidersOnSolved != null)
        {
            foreach (Collider2D col in enableCollidersOnSolved)
            {
                if (col == null)
                    continue;

                if (!value && col == assembleClickArea)
                    continue;

                col.enabled = value;
            }
        }

        if (activateObjectsOnSolved != null)
        {
            foreach (GameObject obj in activateObjectsOnSolved)
            {
                if (obj != null && obj != gameObject)
                    obj.SetActive(value);
            }
        }

        if (activateObjectsOnSolved == null || activateObjectsOnSolved.Length == 0)
            SetAutoSolvedItemActive(value);
    }

    private void SetAutoSolvedItemActive(bool value)
    {
        Transform solvedItem = transform.Find(AutoSolvedItemObjectName);
        if (solvedItem != null)
            solvedItem.gameObject.SetActive(value);
    }

    private void SetPieceCollidersEnabled(bool value)
    {
        CollectChildrenIfEmpty();

        if (pieces == null)
            return;

        foreach (HandPuzzlePiece piece in pieces)
            piece?.SetCollidersEnabled(value);
    }

    private void CollectChildrenIfEmpty()
    {
        if (autoCollectChildren)
        {
            CollectChildren();
            return;
        }

        if (pieces == null || pieces.Length == 0 || slots == null || slots.Length == 0)
            CollectChildren();
    }

    private void CollectChildren()
    {
        pieces = GetComponentsInChildren<HandPuzzlePiece>(true);
        slots = GetComponentsInChildren<HandPuzzleSlot>(true);
    }

    private void ResolveAssembleOutlineTarget()
    {
        if (assembleOutlineTarget == null)
            assembleOutlineTarget = GetComponent<SpriteRendererAssembleOutlineTarget>();

        if (assembleOutlineTarget == null)
            assembleOutlineTarget = GetComponentInChildren<SpriteRendererAssembleOutlineTarget>(true);

        SpriteRenderer source = FindAssembleOutlineSourceRenderer();
        if (source == null)
            return;

        if (assembleOutlineTarget == null && Application.isPlaying)
            assembleOutlineTarget = gameObject.AddComponent<SpriteRendererAssembleOutlineTarget>();

        if (assembleOutlineTarget != null)
            assembleOutlineTarget.Bind(this, source);
    }

    private SpriteRenderer FindAssembleOutlineSourceRenderer()
    {
        Transform hand = transform.Find("Hand");
        if (hand != null && hand.TryGetComponent(out SpriteRenderer handRenderer))
            return handRenderer;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.name.StartsWith("__", System.StringComparison.Ordinal))
                continue;

            if (renderer.GetComponentInParent<HandPuzzlePiece>() != null)
                continue;

            if (renderer.GetComponentInParent<HandPuzzleSlot>() != null)
                continue;

            if (renderer.GetComponent<ToolItem>() != null)
                continue;

            return renderer;
        }

        return null;
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

    private HandPuzzlePiece FindTopPieceUnderMouse(Camera camera)
    {
        CollectChildrenIfEmpty();

        Vector2 mousePosition = camera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        HandPuzzlePiece bestPiece = null;
        int bestSortingOrder = int.MinValue;
        float bestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            HandPuzzlePiece piece = hit.GetComponentInParent<HandPuzzlePiece>();
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

    private bool ContainsPiece(HandPuzzlePiece piece)
    {
        if (piece == null)
            return false;

        if (pieces == null || pieces.Length == 0)
            return true;

        foreach (HandPuzzlePiece candidate in pieces)
        {
            if (candidate == piece)
                return true;
        }

        return false;
    }
}
