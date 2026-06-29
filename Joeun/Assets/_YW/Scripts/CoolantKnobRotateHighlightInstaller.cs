using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CoolantKnobRotateHighlightInstaller : MonoBehaviour
{
    [SerializeField] private bool includeInactive = true;
    [SerializeField, Range(0.5f, 1.2f)] private float circleScale = 0.86f;

    private void Awake()
    {
        Install();
    }

    private void OnEnable()
    {
        Install();
    }

    [ContextMenu("Install Knob Highlights")]
    public void Install()
    {
        CoolantKnob[] knobs = GetComponentsInChildren<CoolantKnob>(includeInactive);
        foreach (CoolantKnob knob in knobs)
        {
            if (knob == null)
                continue;

            RectTransform rect = knob.transform as RectTransform;
            if (rect == null)
                continue;

            SkillHighlightTarget oldHighlight = knob.GetComponent<SkillHighlightTarget>();
            if (oldHighlight != null)
                oldHighlight.enabled = false;

            DisableOldGeneratedHighlight(knob.transform);

            CoolantKnobRotateHighlight highlight = knob.GetComponent<CoolantKnobRotateHighlight>();
            if (highlight == null)
                highlight = knob.gameObject.AddComponent<CoolantKnobRotateHighlight>();

            highlight.Configure(rect, circleScale);

            CoolantKnobRotateGate gate = knob.GetComponent<CoolantKnobRotateGate>();
            if (gate == null)
                gate = knob.gameObject.AddComponent<CoolantKnobRotateGate>();

            gate.Configure(knob);
        }
    }

    private static void DisableOldGeneratedHighlight(Transform target)
    {
        DisableChild(target, "__SkillHighlightFrame");
        DisableChild(target, "__SkillHighlightGlow");
        DisableChild(target, "__SkillHighlightCoreGlow");
    }

    private static void DisableChild(Transform target, string childName)
    {
        if (target == null)
            return;

        Transform child = target.Find(childName);
        if (child != null)
            child.gameObject.SetActive(false);
    }
}

[DisallowMultipleComponent]
public sealed class CoolantKnobRotateGate : MonoBehaviour
{
    [SerializeField] private CoolantKnob knob;

    private CoolantPuzzleManager puzzleManager;

    private void Awake()
    {
        ResolveKnob();
        ResolvePuzzleManager();
        Apply();
    }

    private void OnEnable()
    {
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;
        SkillModeStageRules.OnAvailabilityChanged += HandleAvailabilityChanged;
        Apply();
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;
        SkillModeStageRules.OnAvailabilityChanged -= HandleAvailabilityChanged;
    }

    private void LateUpdate()
    {
        Apply();
    }

    public void Configure(CoolantKnob targetKnob)
    {
        knob = targetKnob;
        Apply();
    }

    private void ResolveKnob()
    {
        if (knob == null)
            knob = GetComponent<CoolantKnob>();
    }

    private void ResolvePuzzleManager()
    {
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<CoolantPuzzleManager>();
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        Apply();
    }

    private void HandleInteractionLockChanged(bool locked)
    {
        Apply();
    }

    private void HandleAvailabilityChanged()
    {
        Apply();
    }

    private void Apply()
    {
        ResolveKnob();
        ResolvePuzzleManager();
        if (knob == null)
            return;

        knob.enabled = IsRotateUsable();
    }

    private bool IsRotateUsable()
    {
        if (puzzleManager != null && puzzleManager.IsSolved)
            return false;

        return SkillIconModeView.CurrentMode == SkillModeType.Rotate
            && !SkillInteractionLock.IsLocked
            && SkillModeStageRules.IsAllowed(SkillModeType.Rotate)
            && PuzzleModeLock.IsAllowedByActiveLocks(SkillModeType.Rotate);
    }
}

[DisallowMultipleComponent]
public sealed class CoolantKnobRotateHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string HighlightObjectName = "__CoolantKnobRotateRing";

    [SerializeField] private RectTransform targetRect;
    [SerializeField, Range(0.5f, 1.2f)] private float circleScale = 0.86f;

    private CoolantKnobRotateRingGraphic ringGraphic;
    private CoolantPuzzleManager puzzleManager;
    private bool hovering;

    private void Awake()
    {
        ResolveTargetRect();
        ResolvePuzzleManager();
        EnsureRing();
    }

    private void OnEnable()
    {
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;
        SkillModeStageRules.OnAvailabilityChanged += HandleAvailabilityChanged;
        Apply();
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;
        SkillModeStageRules.OnAvailabilityChanged -= HandleAvailabilityChanged;

        if (ringGraphic != null)
            ringGraphic.enabled = false;
    }

    private void LateUpdate()
    {
        Apply();
    }

    public void Configure(RectTransform rect, float scale)
    {
        targetRect = rect;
        circleScale = scale;
        EnsureRing();
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        Apply();
    }

    private void ResolveTargetRect()
    {
        if (targetRect == null)
            targetRect = transform as RectTransform;
    }

    private void ResolvePuzzleManager()
    {
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<CoolantPuzzleManager>();
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        Apply();
    }

    private void HandleInteractionLockChanged(bool locked)
    {
        Apply();
    }

    private void HandleAvailabilityChanged()
    {
        Apply();
    }

    private void Apply()
    {
        EnsureRing();
        ResolvePuzzleManager();
        if (ringGraphic == null)
            return;

        bool visible = IsRotateUsable();
        ringGraphic.enabled = visible;
        if (!visible)
            return;

        ringGraphic.SetVisual(
            new Color(1f, 0.95f, 0.12f, hovering ? 1f : 0.92f),
            new Color(1f, 0.92f, 0.08f, hovering ? 0.48f : 0.32f),
            hovering ? 5f : 4f,
            hovering ? 14f : 10f,
            circleScale);
    }

    private void EnsureRing()
    {
        ResolveTargetRect();
        if (targetRect == null)
            return;

        Transform existing = targetRect.Find(HighlightObjectName);
        GameObject ringObject = existing != null
            ? existing.gameObject
            : CreateRingObject(targetRect);

        ringGraphic = ringObject.GetComponent<CoolantKnobRotateRingGraphic>();
        ringObject.SetActive(true);
    }

    private static GameObject CreateRingObject(RectTransform parent)
    {
        GameObject ringObject = new GameObject(HighlightObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CoolantKnobRotateRingGraphic));
        ringObject.transform.SetParent(parent, false);

        CoolantKnobRotateRingGraphic graphic = ringObject.GetComponent<CoolantKnobRotateRingGraphic>();
        graphic.raycastTarget = false;

        RectTransform rect = ringObject.transform as RectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        return ringObject;
    }

    private bool IsRotateUsable()
    {
        if (puzzleManager != null && puzzleManager.IsSolved)
            return false;

        return SkillIconModeView.CurrentMode == SkillModeType.Rotate
            && !SkillInteractionLock.IsLocked
            && SkillModeStageRules.IsAllowed(SkillModeType.Rotate)
            && PuzzleModeLock.IsAllowedByActiveLocks(SkillModeType.Rotate);
    }
}

public sealed class CoolantKnobRotateRingGraphic : MaskableGraphic
{
    private Color lineColor = Color.yellow;
    private Color glowColor = Color.yellow;
    private float lineThickness = 4f;
    private float glowThickness = 10f;
    private float circleScale = 0.86f;

    public void SetVisual(Color line, Color glow, float thickness, float glowSize, float scale)
    {
        lineColor = line;
        glowColor = glow;
        lineThickness = Mathf.Max(0.1f, thickness);
        glowThickness = Mathf.Max(lineThickness, glowSize);
        circleScale = Mathf.Clamp(scale, 0.1f, 1.5f);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f * circleScale;
        if (radius <= 0f)
            return;

        DrawRing(vh, center, radius, glowThickness, glowColor);
        DrawRing(vh, center, radius, lineThickness, lineColor);
    }

    private static void DrawRing(VertexHelper vh, Vector2 center, float radius, float thickness, Color color)
    {
        const int segments = 72;
        float innerRadius = Mathf.Max(0f, radius - thickness * 0.5f);
        float outerRadius = radius + thickness * 0.5f;
        int startIndex = vh.currentVertCount;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * outerRadius, color, Vector2.zero);
            vh.AddVert(center + direction * innerRadius, color, Vector2.zero);
        }

        for (int i = 0; i < segments; i++)
        {
            int index = startIndex + i * 2;
            vh.AddTriangle(index, index + 2, index + 1);
            vh.AddTriangle(index + 2, index + 3, index + 1);
        }
    }
}
