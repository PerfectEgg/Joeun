using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathPuzzleVisual : MonoBehaviour
{
    [System.Serializable]
    private class ToggleVisual
    {
        [SerializeField] private GameObject onVisual;
        [SerializeField] private GameObject offVisual;

        public void SetOn(bool isOn)
        {
            if (onVisual != null)
                onVisual.SetActive(isOn);

            if (offVisual != null)
                offVisual.SetActive(!isOn);
        }
    }

    private class InitialSlotState
    {
        public GridSlot slot;
        public GridNode node;
        public bool hadNode;
        public bool nodeActive;
        public GridNode.Dir direction;
        public Transform parent;
        public int siblingIndex;
    }

    private const string SlotHighlightAreaName = "__PathPuzzleSlotHighlightArea";
    private const string SlotLitOverlayName = "__PathPuzzleLitOverlay";
    private const string TraceErrorOverlayName = "__TraceErrorOverlay";

    [Header("References")]
    [SerializeField] private PathPuzzleManager manager;
    [SerializeField] private Graphic failGlowTarget;
    [SerializeField] private RectTransform failShakeTarget;

    [Header("Object Toggles")]
    [SerializeField] private GameObject[] showOnOpen;
    [SerializeField] private GameObject[] hideOnOpen;
    [SerializeField] private GameObject[] showOnSuccess;
    [SerializeField] private GameObject[] hideOnSuccess;

    [Header("Control Feedback")]
    [SerializeField] private ToggleVisual startVisual;
    [SerializeField] private ToggleVisual resetVisual;
    [SerializeField] private Button[] startButtons;
    [SerializeField] private Button[] resetButtons;
    [SerializeField] private float resetOnSeconds = 1f;

    private bool useSlotAsNodeBackground = true;
    private bool includeGoalSlotInTrace = true;
    private bool autoAddSlotHighlights = true;
    private bool fitNodesToSlots = true;
    private float nodeFillPadding = 0f;
    private bool useNodeSpriteStates = true;
    private Sprite nodeIdleSprite = null;
    private Sprite nodeLitSprite = null;
    [SerializeField] private UnityEngine.Object nodeLitOverlayAsset = null;
    private Sprite nodeStartSprite = null;
    private Sprite nodeGoalSprite = null;
    private float nodeHighlightCornerRadius = -1f;
    private float slotHighlightCornerRadius = -1f;
    private bool autoBindManagerEvents = false;
    private Color failColor = new Color(0.95f, 0.2f, 0.18f);
    private Color traceMissingColor = new Color(1f, 0.08f, 0.08f, 0.86f);
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
    private Coroutine resetPulseRoutine;
    private bool isRunning;
    private Outline failOutline;
    private RectTransform activeShakeTarget;
    private Vector2 activeShakeOriginalPosition;
    private bool hasShakeOriginalPosition;
    private bool ownsInteractionLock;
    private bool isSolved;
    private bool initialStateCaptured;
    private int lastTraceCount;
    private readonly Dictionary<Image, Color> slotIdleColors = new Dictionary<Image, Color>();
    private readonly List<InitialSlotState> initialSlotStates = new List<InitialSlotState>();
    private readonly HashSet<GridNode> initialNodes = new HashSet<GridNode>();
    private Texture2D runtimeNodeLitOverlayTexture;
    private Sprite runtimeNodeLitOverlaySprite;

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

        if (IsSolvedState())
        {
            ApplySolvedVisualState();
        }
        else
        {
            ResetToInitialPuzzleState(true);
        }

        ApplyStartButtonState();
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

        if (resetPulseRoutine != null)
            StopCoroutine(resetPulseRoutine);

        ReleaseInteractionLock();
        RestoreShakeTarget();
        SetStartVisual(false);
        SetResetVisual(false);
    }

    private void OnDestroy()
    {
        ClearRuntimeNodeLitOverlaySprite();
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
        if (!IsSolvedState())
            ResetToInitialPuzzleState(true);

        ApplyStartButtonState();

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
        CaptureInitialStateIfNeeded();
        ResetTraceIndicators();
        ApplyFailGlow(false);
    }

    public void StartTrace()
    {
        if (isRunning || isSolved || manager != null && manager.IsSolved)
        {
            ApplyStartButtonState();
            return;
        }

        Apply();

        if (manager == null)
            return;

        if (manager.IsSolved)
        {
            ApplyStartButtonState();
            return;
        }

        if (visualTraceRoutine != null)
            StopCoroutine(visualTraceRoutine);

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        GameEvent.ESFXPlay?.Invoke("Pattern_Recognition_Play");
        
        SetStartButtonsInteractable(false);
        SetResetButtonsInteractable(true);
        SetStartVisual(true);
        visualTraceRoutine = StartCoroutine(TraceRoutine());
    }

    public void ResetAttemptVisuals()
    {
        if (IsSolvedState())
            return;

        GameEvent.ESFXPlay?.Invoke("Pattern_Recognition_Reset");
        StopActiveTrace();
        StopFeedback();
        ResetToInitialPuzzleState(true);
        ApplyStartButtonState();
        PulseResetVisual();
    }

    private void StopActiveTrace()
    {
        if (visualTraceRoutine != null)
        {
            StopCoroutine(visualTraceRoutine);
            visualTraceRoutine = null;
        }

        isRunning = false;
        ReleaseInteractionLock();
        SetStartVisual(false);
    }

    private void StopFeedback()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        RestoreShakeTarget();
        ApplyFailGlow(false);
    }

    public void PlaySuccessFeedback()
    {
        MarkSolved();

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

        if (!isSolved && (manager == null || !manager.IsSolved))
        {
            SetStartButtonsInteractable(true);
            SetResetButtonsInteractable(true);
        }

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

            PatternPuzzleNodeVisual nodeVisual = ConfigureNodeVisual(node, slot);
            if (nodeVisual != null)
            {
                node.background = null;
            }
            else
            {
                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null)
                {
                    if (!slotIdleColors.ContainsKey(slotImage))
                        slotIdleColors.Add(slotImage, slotImage.color);

                    node.background = slotImage;
                    node.idleColor = slotIdleColors[slotImage];
                }
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

            highlight.SetFrameCornerRadius(slotHighlightCornerRadius);
            highlight.Configure(false, true, false, GetOrCreateSlotHighlightRect(slot));
        }
    }

    private RectTransform GetOrCreateSlotHighlightRect(GridSlot slot)
    {
        if (slot == null)
            return null;

        RectTransform slotRect = slot.transform as RectTransform;
        if (slotRect == null)
            return null;

        Transform existing = slotRect.Find(SlotHighlightAreaName);
        GameObject areaObject = existing != null
            ? existing.gameObject
            : new GameObject(SlotHighlightAreaName, typeof(RectTransform));

        if (existing == null)
            areaObject.transform.SetParent(slotRect, false);

        RectTransform areaRect = areaObject.transform as RectTransform;
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = new Vector2(nodeFillPadding, nodeFillPadding);
        areaRect.offsetMax = new Vector2(-nodeFillPadding, -nodeFillPadding);
        areaRect.pivot = new Vector2(0.5f, 0.5f);
        areaRect.anchoredPosition = Vector2.zero;
        areaRect.localScale = Vector3.one;
        areaObject.transform.SetAsLastSibling();

        return areaRect;
    }

    private void ResetTraceIndicators()
    {
        if (manager == null || manager.traceSlots == null)
            return;

        lastTraceCount = 0;
        HideTraceErrorOverlays();

        for (int i = 0; i < manager.traceSlots.Count; i++)
        {
            Image traceSlot = manager.traceSlots[i];
            if (traceSlot == null)
                continue;

            traceSlot.gameObject.SetActive(true);
            SetTraceSlotAlpha(traceSlot, 0f);
        }
    }

    private void EnsureNodeHighlight(GridNode node)
    {
        if (!autoAddSlotHighlights || node == null)
            return;

        SkillHighlightTarget[] highlights = node.GetComponents<SkillHighlightTarget>();
        if (highlights == null || highlights.Length == 0)
        {
            SkillHighlightTarget highlight = node.gameObject.AddComponent<SkillHighlightTarget>();
            highlights = new[] { highlight };
        }

        RectTransform nodeRect = node.transform as RectTransform;
        foreach (SkillHighlightTarget highlight in highlights)
        {
            if (highlight == null)
                continue;

            highlight.SetFrameCornerRadius(nodeHighlightCornerRadius);
            highlight.Configure(true, false, false, nodeRect);
        }
    }

    private IEnumerator TraceRoutine()
    {
        isRunning = true;
        AcquireInteractionLock();
        ResetTraceIndicators();

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
                SetNodeLit(node, slot, false);

            SetSlotLitOverlay(slot, false);
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

            SetNodeLit(node, slot, true);
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
        visualTraceRoutine = null;
        ReleaseInteractionLock();

        if (success)
        {
            GameEvent.ESFXPlay?.Invoke("Puzzle_Success");
            MarkSolved();
            manager.onSuccess?.Invoke();
        }
        else
        {
            if (string.IsNullOrEmpty(reason))
                reason = count < manager.requiredCount ? "Not enough cells" : "Invalid trace";

            GameEvent.ESFXPlay?.Invoke("Puzzle_Erorr");
            SetStartButtonsInteractable(true);
            SetResetButtonsInteractable(true);
            SetStartVisual(false);
            manager.onFail?.Invoke(reason);
        }
    }

    private void MarkSolved()
    {
        isSolved = true;

        if (manager != null)
            manager.MarkSolved();

        SetStartButtonsInteractable(false);
        SetResetButtonsInteractable(false);
        SetStartVisual(false);
        SetResetVisual(false);
    }

    private void ApplyStartButtonState()
    {
        bool solved = IsSolvedState();
        SetStartButtonsInteractable(!isRunning && !solved);
        SetResetButtonsInteractable(!solved);
        SetStartVisual(isRunning);

        if (solved)
            SetResetVisual(false);
    }

    private bool IsSolvedState()
    {
        return isSolved || manager != null && manager.IsSolved;
    }

    private void SetStartButtonsInteractable(bool interactable)
    {
        if (SetExplicitButtonsInteractable(startButtons, interactable))
            return;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == "Start")
                button.interactable = interactable;
        }
    }

    private void SetResetButtonsInteractable(bool interactable)
    {
        if (SetExplicitButtonsInteractable(resetButtons, interactable))
            return;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == "Reset")
                button.interactable = interactable;
        }
    }

    private bool SetExplicitButtonsInteractable(Button[] buttons, bool interactable)
    {
        if (buttons == null || buttons.Length == 0)
            return false;

        bool hasButton = false;
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.interactable = interactable;
            hasButton = true;
        }

        return hasButton;
    }

    private void SetStartVisual(bool isOn)
    {
        if (startVisual != null)
            startVisual.SetOn(isOn);
    }

    private void SetResetVisual(bool isOn)
    {
        if (resetVisual != null)
            resetVisual.SetOn(isOn);
    }

    private void PulseResetVisual()
    {
        if (resetPulseRoutine != null)
            StopCoroutine(resetPulseRoutine);

        resetPulseRoutine = StartCoroutine(ResetPulseRoutine());
    }

    private IEnumerator ResetPulseRoutine()
    {
        SetResetVisual(true);
        yield return new WaitForSeconds(Mathf.Max(0f, resetOnSeconds));
        SetResetVisual(false);
        resetPulseRoutine = null;
    }

    private void AcquireInteractionLock()
    {
        if (ownsInteractionLock)
            return;

        ownsInteractionLock = true;
        SkillInteractionLock.Push();
    }

    private void ReleaseInteractionLock()
    {
        if (!ownsInteractionLock)
            return;

        ownsInteractionLock = false;
        SkillInteractionLock.Pop();
    }

    private void LightSlot(GridSlot slot)
    {
        GridNode node = slot.currentNode != null
            ? slot.currentNode
            : slot.GetComponentInChildren<GridNode>(true);

        if (node != null)
        {
            SetNodeLit(node, slot, true);
            return;
        }

        if (SetSlotLitOverlay(slot, true))
            return;

        Image slotImage = slot.GetComponent<Image>();
        if (slotImage != null)
            slotImage.color = manager.traceFilledColor;
    }

    private void FillTraceSlot(int count)
    {
        if (manager == null || manager.traceSlots == null)
            return;

        GameEvent.ESFXPlay?.Invoke("Pattern_Recognition_Progress");
        int litCount = Mathf.Clamp(count, 0, manager.requiredCount);
        lastTraceCount = litCount;

        List<Image> orderedSlots = GetTraceSlotsBottomFirst();
        for (int i = 0; i < orderedSlots.Count; i++)
            SetTraceSlotAlpha(orderedSlots[i], i < litCount ? 1f : 0f);
    }

    private bool SetSlotLitOverlay(GridSlot slot, bool lit)
    {
        if (slot == null || !(slot.transform is RectTransform slotRect))
            return false;

        Sprite overlaySprite = ResolveNodeLitOverlaySprite();
        Transform existing = slotRect.Find(SlotLitOverlayName);
        Image overlayImage = existing != null ? existing.GetComponent<Image>() : null;

        if (overlaySprite == null)
        {
            if (overlayImage != null)
                overlayImage.enabled = false;

            return false;
        }

        if (overlayImage == null)
        {
            GameObject overlayObject = existing != null
                ? existing.gameObject
                : new GameObject(SlotLitOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            if (existing == null)
                overlayObject.transform.SetParent(slotRect, false);

            RectTransform overlayRect = overlayObject.transform as RectTransform;
            Stretch(overlayRect);
            overlayImage = overlayObject.GetComponent<Image>();
        }

        overlayImage.sprite = overlaySprite;
        overlayImage.color = Color.white;
        overlayImage.preserveAspect = true;
        overlayImage.raycastTarget = false;
        overlayImage.enabled = lit;
        overlayImage.transform.SetAsLastSibling();
        return true;
    }

    private Sprite ResolveNodeLitOverlaySprite()
    {
        if (nodeLitOverlayAsset is Sprite assetSprite)
            return assetSprite;

        if (nodeLitOverlayAsset is Texture2D assetTexture)
            return GetOrCreateRuntimeNodeLitOverlaySprite(assetTexture);

        return nodeLitSprite;
    }

    private Sprite GetOrCreateRuntimeNodeLitOverlaySprite(Texture2D texture)
    {
        if (texture == null)
            return null;

        if (runtimeNodeLitOverlaySprite != null && runtimeNodeLitOverlayTexture == texture)
            return runtimeNodeLitOverlaySprite;

        ClearRuntimeNodeLitOverlaySprite();
        runtimeNodeLitOverlayTexture = texture;
        runtimeNodeLitOverlaySprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return runtimeNodeLitOverlaySprite;
    }

    private void ClearRuntimeNodeLitOverlaySprite()
    {
        if (runtimeNodeLitOverlaySprite != null)
        {
            Destroy(runtimeNodeLitOverlaySprite);
            runtimeNodeLitOverlaySprite = null;
        }

        runtimeNodeLitOverlayTexture = null;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private IEnumerator SuccessFeedbackRoutine()
    {
        yield return new WaitForSeconds(successDelay);

        ApplySolvedVisualState();
        feedbackRoutine = null;
    }

    private void ApplySolvedVisualState()
    {
        isSolved = true;
        SetObjectsActive(showOnSuccess, true);
        SetObjectsActive(hideOnSuccess, false);
        SetStartButtonsInteractable(false);
        SetResetButtonsInteractable(false);
        SetStartVisual(false);
        SetResetVisual(false);
    }

    private IEnumerator FailFeedbackRoutine()
    {
        int count = Mathf.Max(1, failFlashCount);

        ShowMissingTraceOverlays(lastTraceCount);
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

    private void ResetPuzzleVisuals()
    {
        ResetTraceIndicators();

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
                SetNodeLit(node, slot, false);

            SetSlotLitOverlay(slot, false);
        }
    }

    private void ResetToInitialPuzzleState(bool includeNodeState)
    {
        StopFeedback();
        ApplyFailGlow(false);
        HideTraceErrorOverlays();
        ResetTraceIndicators();

        if (manager == null)
            return;

        if (includeNodeState)
            RestoreInitialNodeState();

        ResetPuzzleVisuals();
    }

    private void CaptureInitialStateIfNeeded()
    {
        if (initialStateCaptured || manager == null)
            return;

        List<GridSlot> slots = manager.slots;
        if (slots == null || slots.Count == 0)
            slots = new List<GridSlot>(manager.GetComponentsInChildren<GridSlot>(true));

        initialSlotStates.Clear();
        initialNodes.Clear();

        foreach (GridSlot slot in slots)
        {
            if (slot == null)
                continue;

            GridNode node = slot.currentNode != null
                ? slot.currentNode
                : slot.GetComponentInChildren<GridNode>(true);

            InitialSlotState state = new InitialSlotState
            {
                slot = slot,
                node = node,
                hadNode = node != null,
                nodeActive = node != null && node.gameObject.activeSelf,
                direction = node != null ? node.direction : GridNode.Dir.Right,
                parent = node != null ? node.transform.parent : null,
                siblingIndex = node != null ? node.transform.GetSiblingIndex() : -1
            };

            initialSlotStates.Add(state);

            if (node != null)
            {
                initialNodes.Add(node);
                slot.currentNode = node;
            }
        }

        initialStateCaptured = true;
    }

    private void RestoreInitialNodeState()
    {
        CaptureInitialStateIfNeeded();

        foreach (InitialSlotState state in initialSlotStates)
        {
            if (state == null || state.slot == null)
                continue;

            GridNode[] childNodes = state.slot.GetComponentsInChildren<GridNode>(true);
            foreach (GridNode childNode in childNodes)
            {
                if (childNode == null || initialNodes.Contains(childNode))
                    continue;

                Destroy(childNode.gameObject);
            }

            if (!state.hadNode || state.node == null)
            {
                state.slot.currentNode = null;
                continue;
            }

            Transform nodeTransform = state.node.transform;
            if (state.parent != null && nodeTransform.parent != state.parent)
                nodeTransform.SetParent(state.parent, false);

            if (nodeTransform.parent != null
                && state.siblingIndex >= 0
                && state.siblingIndex < nodeTransform.parent.childCount)
            {
                nodeTransform.SetSiblingIndex(state.siblingIndex);
            }

            state.node.gameObject.SetActive(state.nodeActive);
            state.slot.currentNode = state.node;
            ApplyNodeDirection(state.node, state.direction);
            SetNodeLit(state.node, state.slot, false);
            SetSlotLitOverlay(state.slot, false);
        }

        BindNodeBackgrounds();
        FitNodesToSlots();
    }

    private static void ApplyNodeDirection(GridNode node, GridNode.Dir direction)
    {
        if (node == null)
            return;

        node.direction = direction;

        Transform target = node.arrow != null ? node.arrow : node.transform;
        target.localEulerAngles = new Vector3(0f, 0f, GridNode.ZAngle(direction));
    }

    private PatternPuzzleNodeVisual ConfigureNodeVisual(GridNode node, GridSlot slot)
    {
        if (!useNodeSpriteStates || node == null)
            return null;

        PatternPuzzleNodeVisual visual = node.GetComponent<PatternPuzzleNodeVisual>();
        if (visual == null)
            visual = node.gameObject.AddComponent<PatternPuzzleNodeVisual>();

        visual.Configure(slot, nodeIdleSprite, nodeLitSprite, nodeStartSprite, nodeGoalSprite, nodeLitOverlayAsset);
        return visual;
    }

    private void SetNodeLit(GridNode node, GridSlot slot, bool lit)
    {
        PatternPuzzleNodeVisual visual = ConfigureNodeVisual(node, slot);
        if (visual != null)
        {
            visual.SetLit(lit);
            return;
        }

        node.SetLit(lit);
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

    private List<Image> GetTraceSlotsBottomFirst()
    {
        List<Image> slots = new List<Image>();
        if (manager == null || manager.traceSlots == null)
            return slots;

        foreach (Image traceSlot in manager.traceSlots)
        {
            if (traceSlot != null)
                slots.Add(traceSlot);
        }

        slots.Sort(CompareTraceSlotBottomFirst);
        return slots;
    }

    private static int CompareTraceSlotBottomFirst(Image a, Image b)
    {
        float ay = a.transform.position.y;
        float by = b.transform.position.y;

        int yCompare = ay.CompareTo(by);
        if (yCompare != 0)
            return yCompare;

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    private static void SetTraceSlotAlpha(Image traceSlot, float alpha)
    {
        if (traceSlot == null)
            return;

        Color color = traceSlot.color;
        color.a = Mathf.Clamp01(alpha);
        traceSlot.color = color;
    }

    private void ShowMissingTraceOverlays(int completedCount)
    {
        HideTraceErrorOverlays();

        if (manager == null || manager.traceSlots == null)
            return;

        List<Image> orderedSlots = GetTraceSlotsBottomFirst();
        int requiredCount = Mathf.Clamp(manager.requiredCount, 0, orderedSlots.Count);
        int startIndex = Mathf.Clamp(completedCount, 0, requiredCount);

        for (int i = startIndex; i < requiredCount; i++)
        {
            Image overlay = GetOrCreateTraceErrorOverlay(orderedSlots[i]);
            if (overlay == null)
                continue;

            overlay.sprite = orderedSlots[i].sprite;
            overlay.type = orderedSlots[i].type;
            overlay.preserveAspect = orderedSlots[i].preserveAspect;
            overlay.fillCenter = orderedSlots[i].fillCenter;
            overlay.fillMethod = orderedSlots[i].fillMethod;
            overlay.fillOrigin = orderedSlots[i].fillOrigin;
            overlay.fillClockwise = orderedSlots[i].fillClockwise;
            overlay.fillAmount = orderedSlots[i].fillAmount;
            overlay.color = traceMissingColor;
            overlay.enabled = true;
            overlay.transform.SetAsLastSibling();
        }
    }

    private void HideTraceErrorOverlays()
    {
        if (manager == null || manager.traceSlots == null)
            return;

        foreach (Image traceSlot in manager.traceSlots)
        {
            if (traceSlot == null)
                continue;

            Transform existing = traceSlot.transform.Find(TraceErrorOverlayName);
            if (existing == null)
                continue;

            Image overlay = existing.GetComponent<Image>();
            if (overlay != null)
                overlay.enabled = false;
        }
    }

    private Image GetOrCreateTraceErrorOverlay(Image traceSlot)
    {
        if (traceSlot == null)
            return null;

        Transform existing = traceSlot.transform.Find(TraceErrorOverlayName);
        Image overlay = existing != null ? existing.GetComponent<Image>() : null;
        if (overlay != null)
            return overlay;

        GameObject overlayObject = existing != null
            ? existing.gameObject
            : new GameObject(TraceErrorOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            overlayObject.transform.SetParent(traceSlot.transform, false);

        RectTransform overlayRect = overlayObject.transform as RectTransform;
        Stretch(overlayRect);

        overlay = overlayObject.GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.enabled = false;
        return overlay;
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
