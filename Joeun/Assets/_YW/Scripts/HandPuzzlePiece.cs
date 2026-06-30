using UnityEngine;

[DisallowMultipleComponent]
public sealed class HandPuzzlePiece : MonoBehaviour, IDraggable
{
    [SerializeField] private string pieceId;
    [SerializeField] private Transform snapPoint;
    [SerializeField] private HandPuzzleController controller;
    [SerializeField] private HandPuzzleDirectionMask openConnections = HandPuzzleDirectionMask.None;
    [SerializeField] private bool returnToStartOnMiss;
    [SerializeField] private bool useSortingOrder = true;
    [SerializeField] private int dragSortingOrder = 999;

    Vector3 initialPosition;
    Vector3 dragStartPosition;
    Vector3 dragOffset;
    SpriteRenderer[] spriteRenderers;
    Color[] spriteBaseColors;
    Collider2D[] colliders;
    int[] sortingOffsets;
    int initialSortingOrder;
    int dragStartSortingOrder;
    bool initialized;
    bool isDragging;
    HandPuzzleSlot currentSlot;
    Collider2D resolvedDragBounds;

    public Vector2 OriginPosition => initialPosition;
    public string PieceId => pieceId;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
    public HandPuzzleSlot CurrentSlot => currentSlot;
    public bool IsSnapped => currentSlot != null;
    public bool CanStartDrag => !isDragging;
    public int PickSortingOrder => CurrentSortingOrder();
    public bool ReturnToStartOnMiss => returnToStartOnMiss;

    private void Awake()
    {
        CacheInitialPosition();
        CacheRenderer();

        if (controller == null)
            controller = GetComponentInParent<HandPuzzleController>();
    }

    private void OnValidate()
    {
        if (snapPoint == null)
            snapPoint = transform;
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
        if (isDragging || !CanStartDrag)
            return;

        if (controller != null && !controller.CanPieceStartDrag(this))
            return;

        CacheInitialPosition();

        if (currentSlot != null)
        {
            currentSlot.ClearOccupant(this);
            currentSlot = null;
            controller?.NotifyPieceReleased(this);
        }

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

        Vector3 targetPosition = new Vector3(
            currentMousePosition.x + dragOffset.x,
            currentMousePosition.y + dragOffset.y,
            transform.position.z);

        transform.position = ClampToDragBounds(targetPosition);
    }

    public void OnDragEnd()
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (controller != null && controller.TrySnap(this, out HandPuzzleSlot slot))
        {
            SnapTo(slot);
            controller.NotifyPieceSnapped(this);
            return;
        }

        if (ShouldReturnToStartOnMiss())
            transform.position = dragStartPosition;

        ApplySortingOrder(dragStartSortingOrder);
    }

    public void SnapTo(HandPuzzleSlot slot)
    {
        if (slot == null)
            return;

        Vector3 delta = slot.SnapWorldPosition - SnapPoint.position;
        transform.position += delta;
        currentSlot = slot;
        currentSlot.SetOccupant(this);
        ApplySortingOrder(slot.SnappedSortingOrder);
    }

    public void ResetToInitial()
    {
        ClearCurrentSlot();
        CacheInitialPosition();
        transform.position = initialPosition;
        isDragging = false;
        ApplySortingOrder(initialSortingOrder);
    }

    public void ClearCurrentSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.ClearOccupant(this);
            currentSlot = null;
        }
    }

    public bool AllowsConnection(HandPuzzleDirection direction)
    {
        return openConnections.Contains(direction);
    }

    public bool HasAllConnections(HandPuzzleDirectionMask requiredConnections)
    {
        return openConnections.ContainsAll(requiredConnections);
    }

    public void SetCollidersEnabled(bool value)
    {
        CacheColliders();

        if (colliders == null)
            return;

        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = value;
        }
    }

    public void ShowWrongFeedback(Color color)
    {
        CacheRenderer();

        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = color;
        }
    }

    public void ClearWrongFeedback()
    {
        CacheRenderer();

        if (spriteRenderers == null || spriteBaseColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length && i < spriteBaseColors.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = spriteBaseColors[i];
        }
    }

    private void CacheInitialPosition()
    {
        if (initialized)
            return;

        initialPosition = transform.position;
        CacheRenderer();
        initialSortingOrder = CurrentSortingOrder();
        initialized = true;
    }

    private void CacheRenderer()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
            return;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        sortingOffsets = new int[spriteRenderers.Length];
        spriteBaseColors = new Color[spriteRenderers.Length];
        int baseOrder = spriteRenderers[0].sortingOrder;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            sortingOffsets[i] = spriteRenderers[i].sortingOrder - baseOrder;
            spriteBaseColors[i] = spriteRenderers[i].color;
        }
    }

    private void CacheColliders()
    {
        if (colliders != null)
            return;

        colliders = GetComponentsInChildren<Collider2D>(true);
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

    private bool ShouldReturnToStartOnMiss()
    {
        return controller == null ? returnToStartOnMiss : controller.ShouldReturnPieceToStartOnMiss(this);
    }

    private Vector3 ClampToDragBounds(Vector3 targetPosition)
    {
        Collider2D bounds = ResolveDragBounds();
        if (bounds == null || !bounds.enabled || !bounds.gameObject.activeInHierarchy)
            return targetPosition;

        Vector2 clamped = bounds.ClosestPoint(targetPosition);
        targetPosition.x = clamped.x;
        targetPosition.y = clamped.y;
        return targetPosition;
    }

    private Collider2D ResolveDragBounds()
    {
        if (resolvedDragBounds != null)
            return resolvedDragBounds;

        Transform current = transform;
        while (current != null)
        {
            Collider2D[] boundsCandidates = current.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D candidate in boundsCandidates)
            {
                if (candidate != null && candidate.name == "HandDragBounds")
                {
                    resolvedDragBounds = candidate;
                    return resolvedDragBounds;
                }
            }

            current = current.parent;
        }

        return null;
    }
}
