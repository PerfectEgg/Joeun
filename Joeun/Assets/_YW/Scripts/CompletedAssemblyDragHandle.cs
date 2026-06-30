using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple UI drag for a completed assembly visual. It does not snap, rotate,
/// unlink connectors, or notify the puzzle manager.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CompletedAssemblyDragHandle : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform dragRoot;

    private RectTransform parentRect;
    private RectTransform dragBoundsRoot;
    private Vector2 dragOffset;
    private bool isDragging;
    private readonly Vector3[] worldCorners = new Vector3[4];

    private void Reset()
    {
        dragRoot = transform as RectTransform;
    }

    private void Awake()
    {
        ResolveReferences();
        DisablePuzzleBehaviours();
    }

    private void OnEnable()
    {
        ResolveReferences();
        DisablePuzzleBehaviours();
    }

    private void OnValidate()
    {
        if (dragRoot == null)
            dragRoot = transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();
        if (dragRoot == null || parentRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        dragOffset = dragRoot.anchoredPosition - localPoint;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragRoot == null || parentRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            dragRoot.anchoredPosition = ClampToDragBounds(localPoint + dragOffset);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void ResolveReferences()
    {
        if (dragRoot == null)
            dragRoot = transform as RectTransform;

        parentRect = dragRoot != null ? dragRoot.parent as RectTransform : null;
    }

    private Vector2 ClampToDragBounds(Vector2 targetPosition)
    {
        if (parentRect == null)
            return targetPosition;

        RectTransform boundsRoot = ResolveDragBoundsRoot();
        if (boundsRoot == null || boundsRoot == parentRect)
        {
            Rect parent = parentRect.rect;
            targetPosition.x = Mathf.Clamp(targetPosition.x, parent.xMin, parent.xMax);
            targetPosition.y = Mathf.Clamp(targetPosition.y, parent.yMin, parent.yMax);
            return targetPosition;
        }

        boundsRoot.GetWorldCorners(worldCorners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 local = parentRect.InverseTransformPoint(worldCorners[i]);
            minX = Mathf.Min(minX, local.x);
            maxX = Mathf.Max(maxX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxY = Mathf.Max(maxY, local.y);
        }

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        return targetPosition;
    }

    private RectTransform ResolveDragBoundsRoot()
    {
        if (dragBoundsRoot != null)
            return dragBoundsRoot;

        Transform current = dragRoot != null ? dragRoot.parent : transform.parent;
        while (current != null)
        {
            RectTransform found = FindRectTransformByName(current, "DragBounds");
            if (found != null && found != dragRoot)
            {
                dragBoundsRoot = found;
                return dragBoundsRoot;
            }

            current = current.parent;
        }

        return parentRect;
    }

    private RectTransform FindRectTransformByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName && root.TryGetComponent(out RectTransform rootRect))
            return rootRect;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform found = FindRectTransformByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void DisablePuzzleBehaviours()
    {
        DisableComponents(GetComponentsInChildren<PuzzlePart>(true));
        DisableComponents(GetComponentsInChildren<PuzzlePartRenderOrderController>(true));
    }

    private static void DisableComponents<T>(T[] components) where T : Behaviour
    {
        if (components == null)
            return;

        foreach (T component in components)
        {
            if (component != null)
                component.enabled = false;
        }
    }
}
