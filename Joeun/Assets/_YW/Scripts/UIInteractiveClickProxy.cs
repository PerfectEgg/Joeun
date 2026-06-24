using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIInteractiveClickProxy : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject target;

    private IInteractive interactive;
    private IHoverable hoverable;

    private void Reset()
    {
        target = gameObject;
    }

    private void Awake()
    {
        CacheTarget();
    }

    private void OnValidate()
    {
        if (target == null)
            target = gameObject;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        CacheTarget();
        interactive?.Interact();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CacheTarget();
        hoverable?.OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CacheTarget();
        hoverable?.OnHoverExit();
    }

    private void CacheTarget()
    {
        GameObject resolvedTarget = target != null ? target : gameObject;
        interactive = resolvedTarget != null ? resolvedTarget.GetComponent<IInteractive>() : null;
        hoverable = resolvedTarget != null ? resolvedTarget.GetComponent<IHoverable>() : null;
    }
}
