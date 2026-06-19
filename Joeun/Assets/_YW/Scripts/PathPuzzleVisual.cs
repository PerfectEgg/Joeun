using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathPuzzleVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathPuzzleManager manager;
    [SerializeField] private Graphic failGlowTarget;
    [SerializeField] private RectTransform failShakeTarget;

    [Header("Object Toggles")]
    [SerializeField] private GameObject[] showOnOpen;
    [SerializeField] private GameObject[] hideOnOpen;
    [SerializeField] private GameObject[] showOnSuccess;
    [SerializeField] private GameObject[] hideOnSuccess;

    private bool hideUnusedTraceSlots = true;
    private bool useSlotAsNodeBackground = true;
    private bool includeGoalSlotInTrace = true;
    private bool autoAddSlotHighlights = true;
    private bool fitNodesToSlots = true;
    private bool autoLayoutTraceSlots = true;
    private float nodeFillPadding = 0f;
    private bool autoBindManagerEvents = false;
    private Color successColor = new Color(0.35f, 0.95f, 0.55f);
    private Color failColor = new Color(0.95f, 0.2f, 0.18f);
    private float successDelay = 1.5f;
    private float failFlashInterval = 0.12f;
    private int failFlashCount = 2;
    private Vector2 failGlowDistance = new Vector2(10f, -10f);
    private float failGlowHoldTime = 0.4f;
    private float failShakeDuration = 0.14f;
    private float failShakeStrength = 5f;
    private bool resetVisualsOnFail = true;

    private Coroutine visualTraceRoutine;
    private Coroutine feedbackRoutine;
    private Coroutine shakeRoutine;
    private bool isRunning;
    private Outline failOutline;
    private RectTransform activeShakeTarget;
    private Vector2 activeShakeOriginalPosition;
    private bool hasShakeOriginalPosition;
    private readonly Dictionary<Image, Color> slotIdleColors = new Dictionary<Image, Color>();

    private void Reset()
    {
        manager = GetComponentInChildren<PathPuzzleManager>(true);
        failShakeTarget = transform as RectTransform;
    }

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
        BindManagerEvents();
    }

    private void OnDisable()
    {
        UnbindManagerEvents();
        isRunning = false;

        if (visualTraceRoutine != null)
            StopCoroutine(visualTraceRoutine);

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        RestoreShakeTarget();
    }

    private void LateUpdate()
    {
        if (manager == null)
            return;

        BindNodeBackgrounds();
        FitNodesToSlots();
    }

    public void OpenPuzzle()
    {
        gameObject.SetActive(true);
        Apply();
        ResetPuzzleVisuals();

        SetObjectsActive(showOnOpen, true);
        SetObjectsActive(hideOnOpen, false);
    }

    public void ClosePuzzle()
    {
        gameObject.SetActive(false);
    }

    [ContextMenu("Apply Path Puzzle View")]
    public void Apply()
    {
        if (manager == null)
            manager = GetComponentInChildren<PathPuzzleManager>(true);

        if (manager == null)
            return;

        BindNodeBackgrounds();
        FitNodesToSlots();
        EnsureSlotHighlights();
        ApplyTraceSlotLimit();
        ApplyFailGlow(false);
    }

    public void StartTrace()
    {
        if (isRunning)
            return;

        Apply();

        if (manager == null)
            return;

        if (visualTraceRoutine != null)
            StopCoroutine(visualTraceRoutine);

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        visualTraceRoutine = StartCoroutine(TraceRoutine());
    }

    public void PlaySuccessFeedback()
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(SuccessFeedbackRoutine());
    }

    public void PlayFailFeedback(string reason)
    {
        if (visualTraceRoutine != null)
            StopCoroutine(visualTraceRoutine);

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FailFeedbackRoutine());
    }

    private void BindNodeBackgrounds()
    {
        if (!useSlotAsNodeBackground)
            return;

        List<GridSlot> slots = manager.slots;
        if (slots == null || slots.Count == 0)
            slots = new List<GridSlot>(manager.GetComponentsInChildren<GridSlot>(true));

        foreach (GridSlot slot in slots)
        {
            if (slot == null)
                continue;

            GridNode node = slot.currentNode != null
                ? slot.currentNode
                : slot.GetComponentInChildren<GridNode>(true);

            if (node == null)
                continue;

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                if (!slotIdleColors.ContainsKey(slotImage))
                    slotIdleColors.Add(slotImage, slotImage.color);

                node.background = slotImage;
                node.idleColor = slotIdleColors[slotImage];
            }

            if (node.arrow == null)
                node.arrow = node.transform as RectTransform;

            slot.currentNode = node;
            EnsureNodeHighlight(node);
        }
    }

    private void FitNodesToSlots()
    {
        if (!fitNodesToSlots)
            return;

        List<GridSlot> slots = manager.slots;
        if (slots == null || slots.Count == 0)
            slots = new List<GridSlot>(manager.GetComponentsInChildren<GridSlot>(true));

        foreach (GridSlot slot in slots)
        {
            if (slot == null)
                continue;

            GridNode node = slot.currentNode != null
                ? slot.currentNode
                : slot.GetComponentInChildren<GridNode>(true);

            if (node == null)
                continue;

            RectTransform nodeRect = node.transform as RectTransform;
            if (nodeRect == null)
                continue;

            nodeRect.anchorMin = Vector2.zero;
            nodeRect.anchorMax = Vector2.one;
            nodeRect.offsetMin = new Vector2(nodeFillPadding, nodeFillPadding);
            nodeRect.offsetMax = new Vector2(-nodeFillPadding, -nodeFillPadding);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.anchoredPosition = Vector2.zero;
            nodeRect.localScale = Vector3.one;

            if (node.arrow == null)
                node.arrow = nodeRect;

            EnsureNodeHighlight(node);
        }
    }

    private void EnsureSlotHighlights()
    {
        if (!autoAddSlotHighlights)
            return;

        List<GridSlot> slots = manager.slots;
        if (slots == null || slots.Count == 0)
            slots = new List<GridSlot>(manager.GetComponentsInChildren<GridSlot>(true));

        foreach (GridSlot slot in slots)
        {
            if (slot == null)
                continue;

            SkillHighlightTarget highlight = slot.GetComponent<SkillHighlightTarget>();
            if (highlight == null)
                highlight = slot.gameObject.AddComponent<SkillHighlightTarget>();

            highlight.Configure(false, true, false, slot.transform as RectTransform);
        }
    }

    private void ApplyTraceSlotLimit()
    {
        if (manager.traceSlots == null)
            return;

        int visibleCount = Mathf.Min(VisibleTraceCount(), manager.traceSlots.Count);
        if (autoLayoutTraceSlots)
            LayoutTraceSlots(visibleCount);

        for (int i = 0; i < manager.traceSlots.Count; i++)
        {
            Image traceSlot = manager.traceSlots[i];
            if (traceSlot == null)
                continue;

            bool used = i < VisibleTraceCount();

            if (hideUnusedTraceSlots)
                traceSlot.gameObject.SetActive(used);

            if (used)
                traceSlot.color = manager.traceEmptyColor;
        }
    }

    private void EnsureNodeHighlight(GridNode node)
    {
        if (!autoAddSlotHighlights || node == null)
            return;

        SkillHighlightTarget highlight = node.GetComponent<SkillHighlightTarget>();
        if (highlight == null)
            highlight = node.gameObject.AddComponent<SkillHighlightTarget>();

        highlight.Configure(true, false, false, node.transform as RectTransform);
    }

    private void LayoutTraceSlots(int visibleCount)
    {
        if (visibleCount <= 0 || manager.traceSlots == null)
            return;

        RectTransform parent = manager.traceSlots[0] != null
            ? manager.traceSlots[0].transform.parent as RectTransform
            : null;

        if (parent == null)
            return;

        float parentHeight = parent.rect.height;
        if (parentHeight <= 0f)
            return;

        float slotHeight = 0f;
        float slotWidth = 0f;
        for (int i = 0; i < manager.traceSlots.Count; i++)
        {
            if (manager.traceSlots[i] == null)
                continue;

            RectTransform rect = manager.traceSlots[i].transform as RectTransform;
            if (rect == null)
                continue;

            slotHeight = rect.rect.height > 0f ? rect.rect.height : rect.sizeDelta.y;
            slotWidth = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
            break;
        }

        if (slotHeight <= 0f)
            slotHeight = parentHeight / visibleCount;

        if (slotWidth <= 0f)
            slotWidth = parent.rect.width;

        float spacing = visibleCount > 1
            ? (parentHeight - slotHeight * visibleCount) / (visibleCount - 1)
            : 0f;

        if (spacing < 0f)
        {
            slotHeight = parentHeight / visibleCount;
            spacing = 0f;
        }

        for (int i = 0; i < manager.traceSlots.Count; i++)
        {
            Image traceSlot = manager.traceSlots[i];
            if (traceSlot == null)
                continue;

            RectTransform rect = traceSlot.transform as RectTransform;
            if (rect == null)
                continue;

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(slotWidth, slotHeight);
            rect.anchoredPosition = new Vector2(0f, i * (slotHeight + spacing));
            rect.localScale = Vector3.one;
        }
    }

    private int VisibleTraceCount()
    {
        return manager.requiredCount;
    }

    private IEnumerator TraceRoutine()
    {
        isRunning = true;
        ApplyTraceSlotLimit();

        List<GridSlot> slots = manager.slots;
        if (slots == null || slots.Count == 0)
            slots = new List<GridSlot>(manager.GetComponentsInChildren<GridSlot>(true));

        Dictionary<Vector2Int, GridSlot> grid = new Dictionary<Vector2Int, GridSlot>();
        Vector2Int startCell = Vector2Int.zero;
        Vector2Int goalCell = Vector2Int.zero;

        foreach (GridSlot slot in slots)
        {
            if (slot == null)
                continue;

            Vector2Int key = new Vector2Int(slot.row, slot.col);
            grid[key] = slot;

            if (slot.isStart)
                startCell = key;

            if (slot.isGoal)
                goalCell = key;

            GridNode node = slot.currentNode != null
                ? slot.currentNode
                : slot.GetComponentInChildren<GridNode>(true);

            if (node != null)
                node.SetLit(false);
        }

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Vector2Int current = startCell;
        int count = 0;
        int guard = manager.rows * manager.cols + 4;
        bool fail = false;
        string reason = "";

        while (guard-- > 0)
        {
            if (!grid.TryGetValue(current, out GridSlot slot) || slot == null)
            {
                fail = true;
                reason = "Grid out";
                break;
            }

            if (current == goalCell)
            {
                if (includeGoalSlotInTrace)
                {
                    LightSlot(slot);
                    FillTraceSlot(++count);
                }

                break;
            }

            if (!visited.Add(current))
            {
                fail = true;
                reason = "Loop";
                break;
            }

            GridNode node = slot.currentNode != null
                ? slot.currentNode
                : slot.GetComponentInChildren<GridNode>(true);

            if (node == null)
            {
                fail = true;
                reason = "Missing node";
                break;
            }

            node.SetLit(true);
            FillTraceSlot(++count);

            if (count > manager.requiredCount)
            {
                fail = true;
                reason = "Too many cells";
                break;
            }

            yield return new WaitForSeconds(manager.stepDelay);

            current += GridNode.Delta(node.direction);
        }

        if (guard <= 0)
        {
            fail = true;
            reason = "Trace guard exceeded";
        }

        bool success = !fail && current == goalCell && count == manager.requiredCount;
        isRunning = false;

        if (success)
        {
            manager.onSuccess?.Invoke();
        }
        else
        {
            if (string.IsNullOrEmpty(reason))
                reason = count < manager.requiredCount ? "Not enough cells" : "Invalid trace";

            manager.onFail?.Invoke(reason);
        }
    }

    private void LightSlot(GridSlot slot)
    {
        Image slotImage = slot.GetComponent<Image>();
        if (slotImage != null)
            slotImage.color = manager.traceFilledColor;
    }

    private void FillTraceSlot(int count)
    {
        int index = count - 1;
        if (manager.traceSlots == null || index < 0 || index >= manager.traceSlots.Count)
            return;

        Image traceSlot = manager.traceSlots[index];
        if (traceSlot != null)
            traceSlot.color = manager.traceFilledColor;
    }

    private IEnumerator SuccessFeedbackRoutine()
    {
        SetVisibleTraceSlotColor(successColor);
        yield return new WaitForSeconds(successDelay);

        SetObjectsActive(showOnSuccess, true);
        SetObjectsActive(hideOnSuccess, false);

    }

    private IEnumerator FailFeedbackRoutine()
    {
        int count = Mathf.Max(1, failFlashCount);

        StartFailShake();

        for (int i = 0; i < count; i++)
        {
            ApplyFailGlow(true);
            yield return new WaitForSeconds(failFlashInterval);
            ApplyFailGlow(false);
            yield return new WaitForSeconds(failFlashInterval);
        }

        ApplyFailGlow(true);
        yield return new WaitForSeconds(failGlowHoldTime);
        ApplyFailGlow(false);

        if (resetVisualsOnFail)
            ResetPuzzleVisuals();
    }

    private void StartFailShake()
    {
        RectTransform target = failShakeTarget != null ? failShakeTarget : transform as RectTransform;
        if (target == null || failShakeDuration <= 0f || failShakeStrength <= 0f)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        RestoreShakeTarget();
        shakeRoutine = StartCoroutine(FailShakeRoutine(target));
    }

    private IEnumerator FailShakeRoutine(RectTransform target)
    {
        activeShakeTarget = target;
        activeShakeOriginalPosition = target.anchoredPosition;
        hasShakeOriginalPosition = true;

        float elapsed = 0f;
        while (elapsed < failShakeDuration)
        {
            float strength = failShakeStrength * (1f - elapsed / failShakeDuration);
            target.anchoredPosition = activeShakeOriginalPosition + Random.insideUnitCircle * strength;

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreShakeTarget();
        shakeRoutine = null;
    }

    private void RestoreShakeTarget()
    {
        if (hasShakeOriginalPosition && activeShakeTarget != null)
            activeShakeTarget.anchoredPosition = activeShakeOriginalPosition;

        activeShakeTarget = null;
        hasShakeOriginalPosition = false;
    }

    private void SetVisibleTraceSlotColor(Color color)
    {
        if (manager == null || manager.traceSlots == null)
            return;

        int count = Mathf.Min(VisibleTraceCount(), manager.traceSlots.Count);
        for (int i = 0; i < count; i++)
        {
            Image traceSlot = manager.traceSlots[i];
            if (traceSlot != null && traceSlot.gameObject.activeSelf)
                traceSlot.color = color;
        }
    }

    private void ResetPuzzleVisuals()
    {
        ApplyTraceSlotLimit();

        if (manager == null)
            return;

        List<GridSlot> slots = manager.slots;
        if (slots == null || slots.Count == 0)
            slots = new List<GridSlot>(manager.GetComponentsInChildren<GridSlot>(true));

        foreach (GridSlot slot in slots)
        {
            if (slot == null)
                continue;

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null && slotIdleColors.TryGetValue(slotImage, out Color idleColor))
                slotImage.color = idleColor;

            GridNode node = slot.currentNode != null
                ? slot.currentNode
                : slot.GetComponentInChildren<GridNode>(true);

            if (node != null)
                node.SetLit(false);
        }
    }

    private void ApplyFailGlow(bool on)
    {
        EnsureFailOutline();

        if (failOutline == null)
            return;

        failOutline.enabled = on;
    }

    private void EnsureFailOutline()
    {
        if (failOutline != null)
            return;

        if (failGlowTarget == null)
            return;

        failOutline = failGlowTarget.GetComponent<Outline>();
        if (failOutline == null)
            failOutline = failGlowTarget.gameObject.AddComponent<Outline>();

        failOutline.effectColor = failColor;
        failOutline.effectDistance = failGlowDistance;
        failOutline.useGraphicAlpha = false;
        failOutline.enabled = false;
    }

    private void BindManagerEvents()
    {
        if (!autoBindManagerEvents)
            return;

        if (manager == null)
            manager = GetComponentInChildren<PathPuzzleManager>(true);

        if (manager == null)
            return;

        manager.onSuccess.RemoveListener(PlaySuccessFeedback);
        manager.onSuccess.AddListener(PlaySuccessFeedback);

        manager.onFail.RemoveListener(PlayFailFeedback);
        manager.onFail.AddListener(PlayFailFeedback);
    }

    private void UnbindManagerEvents()
    {
        if (manager == null)
            return;

        manager.onSuccess.RemoveListener(PlaySuccessFeedback);
        manager.onFail.RemoveListener(PlayFailFeedback);
    }

    private void OnValidate()
    {
        if (failOutline != null)
        {
            failOutline.effectColor = failColor;
            failOutline.effectDistance = failGlowDistance;
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject target in objects)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
