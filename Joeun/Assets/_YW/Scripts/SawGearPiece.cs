using UnityEngine.Serialization;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SawGearPiece : MonoBehaviour, IDraggable
{
    [SerializeField] private string pieceId;
    [SerializeField] private string correctSlotId;
    [FormerlySerializedAs("anchor")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private SawGearPuzzleController controller;
    [SerializeField] private bool returnToStartOnMiss;
    [SerializeField] private bool useSortingOrder = true;
    [SerializeField] private int dragSortingOrder = 999;
    [SerializeField] private Transform rotatingVisualRoot;

    Vector3 initialPosition;
    Quaternion initialRotationRootRotation;
    Vector3 dragStartPosition;
    Vector3 dragOffset;
    SpriteRenderer[] spriteRenderers;
    Color[] originalRendererColors;
    int[] sortingOffsets;
    int initialSortingOrder;
    int dragStartSortingOrder;
    float rotationSpeed;
    bool initialized;
    bool isDragging;
    bool feedbackLocked;
    bool wasSnappedAtDragStart;
    SawGearSlot currentSlot;
    Transform rotationPivot;
    bool hasRotationFollowBase;
    Vector3 rotationFollowBasePosition;
    Quaternion rotationFollowBaseRotation;
    Coroutine wrongFeedbackRoutine;

    public Vector2 OriginPosition => initialPosition;
    public string PieceId => pieceId;
    public string CorrectSlotId => correctSlotId;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
    public Transform Anchor => SnapPoint;
    public SawGearSlot CurrentSlot => currentSlot;
    public bool IsSnapped => currentSlot != null;
    public bool IsCorrectlySnapped => currentSlot != null && currentSlot.SlotId == correctSlotId;
    public bool CanStartDrag => !feedbackLocked;
    public int PickSortingOrder => CurrentSortingOrder();

    private void Awake()
    {
        CacheInitialPosition();
        CacheRenderer();

        if (controller == null)
            controller = GetComponentInParent<SawGearPuzzleController>();
    }

    private void Update()
    {
        if (Mathf.Abs(rotationSpeed) <= 0.001f)
            return;

        RotateBy(rotationSpeed * Time.deltaTime);
    }

    private void OnValidate()
    {
        if (snapPoint == null)
            snapPoint = transform;
    }

    private void OnDisable()
    {
        if (wrongFeedbackRoutine == null)
            return;

        StopCoroutine(wrongFeedbackRoutine);
        wrongFeedbackRoutine = null;
        RestoreOriginalRendererColors();

        if (feedbackLocked)
        {
            feedbackLocked = false;
            ReturnToDragStart();
        }
    }

    private void OnMouseDown()
    {
        OnDragStart();
    }

    private void OnMouseDrag()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        OnDrag(camera.ScreenToWorldPoint(Input.mousePosition));
    }

    private void OnMouseUp()
    {
        OnDragEnd();
    }

    public void OnDragStart()
    {
        if (feedbackLocked || isDragging)
            return;

        if (controller != null && !controller.CanPieceStartDrag(this))
            return;

        CacheInitialPosition();
        wasSnappedAtDragStart = currentSlot != null;

        if (currentSlot != null)
        {
            currentSlot.ClearOccupant(this);
            currentSlot = null;
            rotationPivot = null;
            hasRotationFollowBase = false;
            controller?.NotifyPieceReleased(this);
        }

        SetRotationSpeed(0f);

        Camera camera = Camera.main;
        Vector3 mouseWorld = camera != null
            ? camera.ScreenToWorldPoint(Input.mousePosition)
            : transform.position;
        mouseWorld.z = transform.position.z;

        dragStartPosition = transform.position;
        dragStartSortingOrder = CurrentSortingOrder();
        dragOffset = transform.position - mouseWorld;
        isDragging = true;
        ApplySortingOrder(dragSortingOrder);
    }

    public void OnDrag(Vector2 currentMousePosition)
    {
        if (!isDragging)
            return;

        transform.position = new Vector3(
            currentMousePosition.x + dragOffset.x,
            currentMousePosition.y + dragOffset.y,
            transform.position.z);
    }

    public void OnDragEnd()
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (controller != null && controller.TrySnap(this, out SawGearSlot slot))
        {
            if (!controller.CanPlaceInSlot(this, slot))
            {
                MoveToSlotForFeedback(slot);
                controller.RejectPlacement(this);
                return;
            }

            SnapTo(slot);
            controller.NotifyPieceSnapped(this);
            return;
        }

        if (returnToStartOnMiss && !wasSnappedAtDragStart)
            transform.position = dragStartPosition;

        ApplySortingOrder(dragStartSortingOrder);
    }

    public void SnapTo(SawGearSlot slot)
    {
        if (slot == null)
            return;

        SetRotationSpeed(0f);
        RotationRoot.rotation = initialRotationRootRotation;

        Vector3 delta = slot.SnapWorldPosition - SnapPoint.position;
        transform.position += delta;
        currentSlot = slot;
        rotationPivot = slot.RotationPivot;
        BeginRotationFollow(slot.CurrentRotationAngle, slot.SnapWorldPosition);
        currentSlot.SetOccupant(this);
        ApplySortingOrder(slot.SnappedSortingOrder);
    }

    public void MoveToSlotForFeedback(SawGearSlot slot)
    {
        if (slot == null)
            return;

        ClearCurrentSlot();
        SetRotationSpeed(0f);
        RotationRoot.rotation = initialRotationRootRotation;

        Vector3 delta = slot.SnapWorldPosition - SnapPoint.position;
        transform.position += delta;
        rotationPivot = null;
        hasRotationFollowBase = false;
        ApplySortingOrder(dragSortingOrder);
    }

    public void SetRotationSpeed(float degreesPerSecond)
    {
        rotationSpeed = degreesPerSecond;
    }

    public void RotateBy(float degrees)
    {
        Vector3 pivotPosition = rotationPivot != null ? rotationPivot.position : Anchor.position;
        RotateBy(degrees, pivotPosition);
    }

    public void RotateBy(float degrees, Vector3 pivotPosition)
    {
        if (Mathf.Abs(degrees) <= 0.001f)
            return;

        RotationRoot.RotateAround(pivotPosition, Vector3.forward, degrees);
    }

    public void FollowRotation(float degrees, Vector3 pivotPosition)
    {
        if (!hasRotationFollowBase)
            CaptureRotationFollowBase();

        RotationRoot.position = rotationFollowBasePosition;
        RotationRoot.rotation = rotationFollowBaseRotation;

        if (Mathf.Abs(degrees) > 0.001f)
            RotationRoot.RotateAround(pivotPosition, Vector3.forward, degrees);
    }

    public void PlayWrongFeedback(Color color, float duration)
    {
        if (wrongFeedbackRoutine != null)
            StopCoroutine(wrongFeedbackRoutine);

        wrongFeedbackRoutine = StartCoroutine(WrongFeedbackRoutine(color, duration));
    }

    public void ReturnToDragStart()
    {
        ClearCurrentSlot();
        transform.position = dragStartPosition;
        SetRotationSpeed(0f);
        ApplySortingOrder(dragStartSortingOrder);
        controller?.NotifyPieceReleased(this);
    }

    public void ResetToInitial()
    {
        ClearCurrentSlot();
        CacheInitialPosition();
        transform.position = initialPosition;
        isDragging = false;
        feedbackLocked = false;
        SetRotationSpeed(0f);
        ApplySortingOrder(initialSortingOrder);
    }

    public void ClearCurrentSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.ClearOccupant(this);
            currentSlot = null;
        }

        rotationPivot = null;
        hasRotationFollowBase = false;
        SetRotationSpeed(0f);
    }

    private void CacheInitialPosition()
    {
        if (initialized)
            return;

        initialPosition = transform.position;
        CacheRenderer();
        initialSortingOrder = CurrentSortingOrder();
        initialRotationRootRotation = RotationRoot.rotation;
        initialized = true;
    }

    private void CacheRenderer()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
            return;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        originalRendererColors = new Color[spriteRenderers.Length];
        sortingOffsets = new int[spriteRenderers.Length];
        int baseOrder = spriteRenderers[0].sortingOrder;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalRendererColors[i] = spriteRenderers[i].color;
            sortingOffsets[i] = spriteRenderers[i].sortingOrder - baseOrder;
        }
    }

    private int CurrentSortingOrder()
    {
        CacheRenderer();
        return spriteRenderers != null && spriteRenderers.Length > 0 ? spriteRenderers[0].sortingOrder : 0;
    }

    private void ApplySortingOrder(int order)
    {
        if (!useSortingOrder)
            return;

        CacheRenderer();
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].sortingOrder = order + (sortingOffsets != null ? sortingOffsets[i] : 0);
        }
    }

    private Transform RotationRoot => rotatingVisualRoot != null ? rotatingVisualRoot : transform;

    private void BeginRotationFollow(float degrees, Vector3 pivotPosition)
    {
        CaptureRotationFollowBase();
        FollowRotation(degrees, pivotPosition);
    }

    private void CaptureRotationFollowBase()
    {
        rotationFollowBasePosition = RotationRoot.position;
        rotationFollowBaseRotation = RotationRoot.rotation;
        hasRotationFollowBase = true;
    }

    private System.Collections.IEnumerator WrongFeedbackRoutine(Color color, float duration)
    {
        feedbackLocked = true;
        CacheRenderer();

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    spriteRenderers[i].color = color;
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        RestoreOriginalRendererColors();

        ReturnToDragStart();
        feedbackLocked = false;
        wrongFeedbackRoutine = null;
    }

    private void RestoreOriginalRendererColors()
    {
        CacheRenderer();

        if (spriteRenderers == null || originalRendererColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalRendererColors.Length)
                spriteRenderers[i].color = originalRendererColors[i];
        }
    }
}
