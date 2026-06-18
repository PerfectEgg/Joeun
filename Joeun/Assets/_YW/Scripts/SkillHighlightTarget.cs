using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillHighlightTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool highlightOnRotate = true;
    [SerializeField] private bool highlightOnAssemble;
    [SerializeField] private bool highlightOnDecode;

    [Tooltip("비워두면 이 오브젝트의 RectTransform 크기를 사용합니다.")]
    [SerializeField] private RectTransform highlightRect;

    [Header("Colors")]
    [SerializeField] private Color rotateColor = new Color(1f, 0.95f, 0.12f, 1f);
    [SerializeField] private Color assembleColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private Color decodeColor = new Color(1f, 0.12f, 0.82f, 1f);

    [Header("Frame")]
    [SerializeField] private float borderThickness = 4f;
    [SerializeField] private float hoverBorderThickness = 5f;
    [SerializeField] private float borderAlpha = 0.9f;
    [SerializeField] private float hoverBorderAlpha = 1f;

    [Header("Outer Glow")]
    [SerializeField] private float outerGlowSize = 10f;
    [SerializeField] private float hoverOuterGlowSize = 15f;
    [SerializeField] private float outerGlowAlpha = 0.34f;
    [SerializeField] private float hoverOuterGlowAlpha = 0.52f;
    [SerializeField] private int outerGlowSteps = 5;

    [Header("Sprite Tint")]
    [SerializeField] private float spriteIdleTint = 0.04f;
    [SerializeField] private float spriteHoverTint = 0.1f;

    [Header("Transition")]
    [SerializeField] private float fadeInDuration = 0.08f;
    [SerializeField] private float fadeOutDuration = 0.08f;

    private const string FrameObjectName = "__SkillHighlightFrame";
    private const string OldGlowObjectName = "__SkillHighlightGlow";
    private const string OldCoreGlowObjectName = "__SkillHighlightCoreGlow";

    private SkillHighlightFrameGraphic frameGraphic;
    private readonly Dictionary<SpriteRenderer, Color> spriteBaseColors = new Dictionary<SpriteRenderer, Color>();
    private SpriteRenderer[] spriteRenderers;
    private bool isHovering;
    private Coroutine fadeRoutine;

    public void Configure(bool rotate, bool assemble, bool decode, RectTransform rect = null)
    {
        highlightOnRotate = rotate;
        highlightOnAssemble = assemble;
        highlightOnDecode = decode;

        if (rect != null)
            highlightRect = rect;

        if (isActiveAndEnabled)
        {
            EnsureFrame();
            Apply(SkillIconModeView.CurrentMode);
        }
    }

    private void Reset()
    {
        highlightRect = transform as RectTransform;
        GuessSkillFromComponents();
    }

    private void Awake()
    {
        if (highlightRect == null)
            highlightRect = transform as RectTransform;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnEnable()
    {
        EnsureFrame();
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        Apply(SkillIconModeView.CurrentMode);
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        StopFrameFade();
        Clear();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        Apply(SkillIconModeView.CurrentMode);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        Apply(SkillIconModeView.CurrentMode);
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        Apply(mode);
    }

    private void Apply(SkillModeType mode)
    {
        if (!Matches(mode))
        {
            Clear();
            return;
        }

        EnsureFrame();

        Color skillColor = ColorFor(mode);
        ApplyFrame(skillColor);
        ApplySpriteTint(skillColor, isHovering ? spriteHoverTint : spriteIdleTint);
    }

    private void ApplyFrame(Color skillColor)
    {
        if (frameGraphic == null)
            return;

        Color borderColor = Color.Lerp(Color.white, skillColor, 0.35f);
        borderColor.a = isHovering ? hoverBorderAlpha : borderAlpha;

        Color glowColor = skillColor;
        glowColor.a = isHovering ? hoverOuterGlowAlpha : outerGlowAlpha;

        frameGraphic.SetVisual(
            borderColor,
            glowColor,
            isHovering ? hoverBorderThickness : borderThickness,
            isHovering ? hoverOuterGlowSize : outerGlowSize,
            outerGlowSteps);

        ShowFrame();
    }

    private void Clear()
    {
        if (frameGraphic != null)
            HideFrame();

        foreach (KeyValuePair<SpriteRenderer, Color> entry in spriteBaseColors)
        {
            if (entry.Key != null)
                entry.Key.color = entry.Value;
        }
    }

    private void ShowFrame()
    {
        if (frameGraphic == null)
            return;

        StopFrameFade();

        bool wasDisabled = !frameGraphic.enabled;
        frameGraphic.enabled = true;

        if (wasDisabled)
            frameGraphic.canvasRenderer.SetAlpha(0f);

        frameGraphic.CrossFadeAlpha(1f, fadeInDuration, true);
    }

    private void HideFrame()
    {
        if (frameGraphic == null)
            return;

        StopFrameFade();

        if (!isActiveAndEnabled || fadeOutDuration <= 0f)
        {
            frameGraphic.enabled = false;
            return;
        }

        fadeRoutine = StartCoroutine(HideFrameRoutine());
    }

    private IEnumerator HideFrameRoutine()
    {
        frameGraphic.CrossFadeAlpha(0f, fadeOutDuration, true);
        yield return new WaitForSeconds(fadeOutDuration);

        if (frameGraphic != null)
            frameGraphic.enabled = false;

        fadeRoutine = null;
    }

    private void StopFrameFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private bool Matches(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                return highlightOnRotate;
            case SkillModeType.Assemble:
                if (!highlightOnAssemble)
                    return false;

                GridSlot slot = GetComponent<GridSlot>();
                if (slot == null)
                    slot = GetComponentInParent<GridSlot>();

                return slot == null || slot.assemblable && slot.currentNode == null;
            case SkillModeType.Decode:
                return highlightOnDecode;
            default:
                return false;
        }
    }

    private Color ColorFor(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                return rotateColor;
            case SkillModeType.Assemble:
                return assembleColor;
            case SkillModeType.Decode:
                return decodeColor;
            default:
                return Color.clear;
        }
    }

    private void ApplySpriteTint(Color color, float amount)
    {
        if (spriteRenderers == null)
            return;

        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null)
                continue;

            if (!spriteBaseColors.TryGetValue(renderer, out Color baseColor))
            {
                baseColor = renderer.color;
                spriteBaseColors.Add(renderer, baseColor);
            }

            renderer.color = Color.Lerp(baseColor, color, amount);
        }
    }

    private void EnsureFrame()
    {
        if (highlightRect == null)
            highlightRect = transform as RectTransform;

        if (highlightRect == null)
            return;

        DisableOldGeneratedGlow(highlightRect);

        Transform existing = highlightRect.Find(FrameObjectName);
        GameObject frameObject = existing != null
            ? existing.gameObject
            : CreateFrameObject(highlightRect);

        RectTransform rect = frameObject.transform as RectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        frameObject.transform.SetAsLastSibling();
        frameGraphic = frameObject.GetComponent<SkillHighlightFrameGraphic>();
        frameGraphic.raycastTarget = false;
        frameGraphic.maskable = false;
    }

    private GameObject CreateFrameObject(RectTransform parent)
    {
        GameObject frameObject = new GameObject(FrameObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(SkillHighlightFrameGraphic));
        frameObject.transform.SetParent(parent, false);

        SkillHighlightFrameGraphic graphic = frameObject.GetComponent<SkillHighlightFrameGraphic>();
        graphic.enabled = false;
        graphic.canvasRenderer.SetAlpha(0f);

        return frameObject;
    }

    private static void DisableOldGeneratedGlow(Transform target)
    {
        if (target == null)
            return;

        Transform oldGlow = target.Find(OldGlowObjectName);
        if (oldGlow != null)
            oldGlow.gameObject.SetActive(false);

        Transform oldCoreGlow = target.Find(OldCoreGlowObjectName);
        if (oldCoreGlow != null)
            oldCoreGlow.gameObject.SetActive(false);
    }

    private void GuessSkillFromComponents()
    {
        if (GetComponent<GridNode>() != null || GetComponent<PuzzlePart>() != null)
        {
            highlightOnRotate = true;
            highlightOnAssemble = false;
            highlightOnDecode = false;
            return;
        }

        GridSlot slot = GetComponent<GridSlot>();
        if (slot != null)
        {
            highlightOnRotate = false;
            highlightOnAssemble = slot.assemblable && slot.currentNode == null;
            highlightOnDecode = false;
        }
    }
}

public class SkillHighlightFrameGraphic : MaskableGraphic
{
    private Color borderColor = Color.white;
    private Color glowColor = Color.yellow;
    private float borderThickness = 3f;
    private float outerGlowSize = 10f;
    private int outerGlowSteps = 5;

    public void SetVisual(Color border, Color glow, float borderWidth, float glowSize, int glowSteps)
    {
        borderColor = border;
        glowColor = glow;
        borderThickness = Mathf.Max(0f, borderWidth);
        outerGlowSize = Mathf.Max(0f, glowSize);
        outerGlowSteps = Mathf.Max(1, glowSteps);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        if (outerGlowSize > 0f && glowColor.a > 0f)
        {
            float step = outerGlowSize / outerGlowSteps;
            for (int i = outerGlowSteps - 1; i >= 0; i--)
            {
                float innerOffset = borderThickness + i * step;
                float outerOffset = borderThickness + (i + 1) * step;
                float t = 1f - (float)i / outerGlowSteps;

                Color c = glowColor;
                c.a *= t * t;

                AddRing(vh, Expand(rect, outerOffset), Expand(rect, innerOffset), c);
            }
        }

        if (borderThickness > 0f && borderColor.a > 0f)
            AddRing(vh, rect, Inset(rect, borderThickness), borderColor);
    }

    private static Rect Expand(Rect rect, float amount)
    {
        rect.xMin -= amount;
        rect.xMax += amount;
        rect.yMin -= amount;
        rect.yMax += amount;
        return rect;
    }

    private static Rect Inset(Rect rect, float amount)
    {
        rect.xMin += amount;
        rect.xMax -= amount;
        rect.yMin += amount;
        rect.yMax -= amount;
        return rect;
    }

    private static void AddRing(VertexHelper vh, Rect outer, Rect inner, Color color)
    {
        AddQuad(vh, outer.xMin, inner.yMax, outer.xMax, outer.yMax, color);
        AddQuad(vh, outer.xMin, outer.yMin, outer.xMax, inner.yMin, color);
        AddQuad(vh, outer.xMin, inner.yMin, inner.xMin, inner.yMax, color);
        AddQuad(vh, inner.xMax, inner.yMin, outer.xMax, inner.yMax, color);
    }

    private static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Color color)
    {
        if (xMax <= xMin || yMax <= yMin || color.a <= 0f)
            return;

        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(xMin, yMin);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMin, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMin);
        vh.AddVert(vertex);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
