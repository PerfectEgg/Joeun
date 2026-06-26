using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemDropRouterTarget : MonoBehaviour, IPickable
{
    [Serializable]
    private sealed class Route
    {
        [SerializeField] private string itemId;
        [SerializeField] private MonoBehaviour target;

        public bool Matches(string keyId)
        {
            return !string.IsNullOrEmpty(itemId) && itemId == keyId;
        }

        public bool IsLocked
        {
            get
            {
                return target is IPickable pickable && pickable.IsLocked;
            }
        }

        public bool TryUnlock(string keyId, UnityEngine.Object context)
        {
            if (target == null)
            {
                Debug.LogWarning($"[ItemDropRouterTarget] Route target is missing for item '{itemId}'.", context);
                return false;
            }

            if (target is IPickable pickable)
                return pickable.TryUnlock(keyId);

            Debug.LogWarning($"[ItemDropRouterTarget] '{target.name}' does not implement IPickable.", target);
            return false;
        }
    }

    [SerializeField] private Route[] routes = Array.Empty<Route>();
    [SerializeField] private bool logUnknownItem = true;

    public bool IsLocked
    {
        get
        {
            foreach (Route route in routes)
            {
                if (route != null && route.IsLocked)
                    return true;
            }

            return false;
        }
    }

    public bool TryUnlock(string keyId)
    {
        foreach (Route route in routes)
        {
            if (route != null && route.Matches(keyId))
                return route.TryUnlock(keyId, this);
        }

        if (logUnknownItem)
            Debug.Log($"[ItemDropRouterTarget] No route for item '{keyId}'.", this);

        return false;
    }
}
