using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages multiple joint visual swaps for the connect puzzle from one inspector.
/// Attach this to ConnectManager or the ConnectPuzzle root.
/// </summary>
public class ConnectJointVisualSwapGroup : MonoBehaviour
{
    [Serializable]
    public class JointSwap
    {
        public string label;

        [Header("Connection")]
        public ConnectorPoint connector;
        public ConnectorPoint expectedLinkedTo;

        [Header("Visuals")]
        public GameObject[] showWhenDisconnected;
        public GameObject[] showWhenConnected;

        [NonSerialized] public bool hasState;
        [NonSerialized] public bool lastConnected;
        [NonSerialized] public Coroutine pendingRoutine;
        [NonSerialized] public Coroutine fadeRoutine;
    }

    bool initializeOnEnable = true;
    float connectedVisualDelay = 0.12f;
    float connectedFadeDuration = 0.24f;
    [SerializeField] JointSwap[] swaps;

    void OnEnable()
    {
        if (!initializeOnEnable) return;

        ResetStateCache();
        RefreshAll(true);
    }

    void Update()
    {
        RefreshAll(false);
    }

    void OnDisable()
    {
        StopPendingSwaps();
    }

    [ContextMenu("Refresh Joint Visuals")]
    public void RefreshNow()
    {
        RefreshAll(true);
    }

    [ContextMenu("Reset State Cache")]
    public void ResetStateCache()
    {
        if (swaps == null) return;

        foreach (var swap in swaps)
        {
            if (swap == null) continue;
            StopPendingSwap(swap);
            StopFadeSwap(swap);
            swap.hasState = false;
        }
    }

    void RefreshAll(bool force)
    {
        if (swaps == null) return;

        foreach (var swap in swaps)
            Refresh(swap, force);
    }

    void Refresh(JointSwap swap, bool force)
    {
        if (swap == null) return;

        bool connected = IsExpectedConnectionActive(swap);
        if (!force && swap.hasState && connected == swap.lastConnected) return;

        StopPendingSwap(swap);
        StopFadeSwap(swap);

        if (connected && !force && connectedVisualDelay > 0f)
        {
            ApplyVisualState(swap, false);
            swap.pendingRoutine = StartCoroutine(ApplyConnectedAfterDelay(swap, connectedVisualDelay));
        }
        else
        {
            ApplyVisualState(swap, connected);
        }

        swap.lastConnected = connected;
        swap.hasState = true;
    }

    IEnumerator ApplyConnectedAfterDelay(JointSwap swap, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (swap != null && IsExpectedConnectionActive(swap))
            ApplyConnectedVisual(swap, true);

        if (swap != null)
            swap.pendingRoutine = null;
    }

    void StopPendingSwaps()
    {
        if (swaps == null) return;

        foreach (var swap in swaps)
        {
            if (swap != null)
                StopPendingSwap(swap);
        }
    }

    void StopPendingSwap(JointSwap swap)
    {
        if (swap.pendingRoutine == null) return;

        StopCoroutine(swap.pendingRoutine);
        swap.pendingRoutine = null;
    }

    void StopFadeSwap(JointSwap swap)
    {
        if (swap.fadeRoutine == null) return;

        StopCoroutine(swap.fadeRoutine);
        swap.fadeRoutine = null;
    }

    void ApplyVisualState(JointSwap swap, bool connected)
    {
        if (connected)
            ApplyConnectedVisual(swap, false);
        else
            ApplyDisconnectedVisual(swap);
    }

    void ApplyConnectedVisual(JointSwap swap, bool animate)
    {
        SetObjects(swap.showWhenDisconnected, false);

        if (animate && connectedFadeDuration > 0f)
        {
            SetAlpha(swap.showWhenConnected, 0f);
            SetObjects(swap.showWhenConnected, true);
            swap.fadeRoutine = StartCoroutine(FadeObjects(swap, swap.showWhenConnected, 0f, 1f, connectedFadeDuration));
        }
        else
        {
            SetAlpha(swap.showWhenConnected, 1f);
            SetObjects(swap.showWhenConnected, true);
        }
    }

    void ApplyDisconnectedVisual(JointSwap swap)
    {
        SetAlpha(swap.showWhenDisconnected, 1f);
        SetObjects(swap.showWhenDisconnected, true);

        SetAlpha(swap.showWhenConnected, 1f);
        SetObjects(swap.showWhenConnected, false);
    }

    IEnumerator FadeObjects(JointSwap swap, GameObject[] objects, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(objects, Mathf.Lerp(from, to, Smooth01(t)));
            yield return null;
        }

        SetAlpha(objects, to);

        if (swap != null)
            swap.fadeRoutine = null;
    }

    static float Smooth01(float t)
    {
        return t * t * (3f - 2f * t);
    }

    static bool IsExpectedConnectionActive(JointSwap swap)
    {
        if (swap.connector == null || !swap.connector.IsLinked)
            return false;

        if (swap.expectedLinkedTo == null)
            return false;

        return swap.connector.linkedTo == swap.expectedLinkedTo
            || swap.expectedLinkedTo.linkedTo == swap.connector;
    }

    static void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }
    }

    static void SetAlpha(GameObject[] objects, float alpha)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj == null) continue;

            foreach (var graphic in obj.GetComponentsInChildren<Graphic>(true))
            {
                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }

            foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
    }
}
