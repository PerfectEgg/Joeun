using UnityEngine;

[DisallowMultipleComponent]
public class InteractionHitboxProxy : MonoBehaviour, IInteractive, IHoverable, IDraggable
{
    [SerializeField] private GameObject target;

    IInteractive interactive;
    IHoverable hoverable;
    IDraggable draggable;

    public Vector2 OriginPosition
    {
        get
        {
            CacheTarget();
            return draggable != null ? draggable.OriginPosition : (Vector2)transform.position;
        }
    }

    void Awake()
    {
        CacheTarget();
    }

    void OnValidate()
    {
        if (target == null && transform.parent != null)
            target = transform.parent.gameObject;
    }

    public void Interact()
    {
        CacheTarget();
        interactive?.Interact();
    }

    public void OnHoverEnter()
    {
        CacheTarget();
        hoverable?.OnHoverEnter();
    }

    public void OnHoverExit()
    {
        CacheTarget();
        hoverable?.OnHoverExit();
    }

    public void OnDragStart()
    {
        CacheTarget();
        draggable?.OnDragStart();
    }

    public void OnDrag(Vector2 currentMousePosition)
    {
        CacheTarget();
        draggable?.OnDrag(currentMousePosition);
    }

    public void OnDragEnd()
    {
        CacheTarget();
        draggable?.OnDragEnd();
    }

    void CacheTarget()
    {
        GameObject resolvedTarget = ResolveTarget();
        if (resolvedTarget == null)
            return;

        interactive = FindInterface<IInteractive>(resolvedTarget);
        hoverable = FindInterface<IHoverable>(resolvedTarget);
        draggable = FindInterface<IDraggable>(resolvedTarget);
    }

    GameObject ResolveTarget()
    {
        if (target != null)
            return target;

        return transform.parent != null ? transform.parent.gameObject : null;
    }

    T FindInterface<T>(GameObject root) where T : class
    {
        if (root == null)
            return null;

        Transform current = root.transform;
        while (current != null)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour == this)
                    continue;

                if (behaviour is T matched)
                    return matched;
            }

            current = current.parent;
        }

        return null;
    }
}
