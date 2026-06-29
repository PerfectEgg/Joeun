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
    private Vector2 dragOffset;
    private bool isDragging;

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
            dragRoot.anchoredPosition = localPoint + dragOffset;
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
