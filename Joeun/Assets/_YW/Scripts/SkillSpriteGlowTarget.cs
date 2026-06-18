using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SkillSpriteGlowTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Mode")]
    [SerializeField] private bool highlightOnRotate;
    [SerializeField] private bool highlightOnAssemble = true;
    [SerializeField] private bool highlightOnDecode;

    [Header("Colors")]
    [SerializeField] private Color rotateColor = new Color(1f, 0.95f, 0.12f, 1f);
    [SerializeField] private Color assembleColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private Color decodeColor = new Color(1f, 0.12f, 0.82f, 1f);

    [Header("Back Image Glow")]
    [SerializeField] private float activationDelay = 0.22f;
    [SerializeField] private float idleAlpha = 0.36f;
    [SerializeField] private float hoverAlpha = 0.62f;
    [SerializeField] private float idleScale = 1.05f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float fadeDuration = 0.08f;
    [SerializeField] private bool keepBehindTarget = true;

    private const string GlowName = "__SkillSpriteGlow";

    private Image targetImage;
    private Image glowImage;
    private RectTransform targetRect;
    private RectTransform glowRect;
    private bool isHovering;
    private bool waitingForActivation;
    private float activationTime;
    private float currentAlpha;
    private float targetAlpha;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        targetRect = transform as RectTransform;
        EnsureGlow();
    }

    private void OnEnable()
    {
        EnsureGlow();
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;

        waitingForActivation = activationDelay > 0f;
        activationTime = Time.unscaledTime + activationDelay;
        SetGlowAlpha(0f, true);

        if (!waitingForActivation)
            Apply(SkillIconModeView.CurrentMode, true);
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SetGlowAlpha(0f, true);
    }

    private void LateUpdate()
    {
        SyncGlowTransform();

        if (waitingForActivation)
        {
            if (Time.unscaledTime < activationTime)
            {
                SetGlowAlpha(0f, true);
                return;
            }

            waitingForActivation = false;
            Apply(SkillIconModeView.CurrentMode, false);
        }

        if (Mathf.Approximately(currentAlpha, targetAlpha))
            return;

        float step = fadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / fadeDuration;
        SetGlowAlpha(Mathf.MoveTowards(currentAlpha, targetAlpha, step), true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        Apply(SkillIconModeView.CurrentMode, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        Apply(SkillIconModeView.CurrentMode, false);
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        if (waitingForActivation)
            return;

        Apply(mode, false);
    }

    private void Apply(SkillModeType mode, bool immediate)
    {
        EnsureGlow();

        if (!Matches(mode) || targetImage == null || targetImage.sprite == null)
        {
            SetGlowAlpha(0f, immediate);
            return;
        }

        Color color = ColorFor(mode);
        color.a = 1f;
        glowImage.color = color;
        glowImage.sprite = targetImage.sprite;
        glowImage.type = targetImage.type;
        glowImage.preserveAspect = targetImage.preserveAspect;

        SetGlowAlpha(isHovering ? hoverAlpha : idleAlpha, immediate);
    }

    private bool Matches(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                return highlightOnRotate;
            case SkillModeType.Assemble:
                return highlightOnAssemble;
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

    private void EnsureGlow()
    {
        if (targetRect == null)
            targetRect = transform as RectTransform;

        if (targetRect == null)
            return;

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (glowImage != null)
            return;

        Transform parent = transform.parent;
        Transform existing = parent != null ? parent.Find(GlowName + "_" + name) : null;
        GameObject glowObject = existing != null ? existing.gameObject : new GameObject(GlowName + "_" + name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            glowObject.transform.SetParent(parent, false);

        glowRect = glowObject.transform as RectTransform;
        glowImage = glowObject.GetComponent<Image>();
        glowImage.raycastTarget = false;
        glowImage.maskable = targetImage == null || targetImage.maskable;

        if (keepBehindTarget)
            glowObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

        SyncGlowTransform();
        SetGlowAlpha(0f, true);
    }

    private void SyncGlowTransform()
    {
        if (targetRect == null || glowRect == null)
            return;

        glowRect.anchorMin = targetRect.anchorMin;
        glowRect.anchorMax = targetRect.anchorMax;
        glowRect.pivot = targetRect.pivot;
        glowRect.anchoredPosition = targetRect.anchoredPosition;
        glowRect.sizeDelta = targetRect.sizeDelta;
        glowRect.localRotation = targetRect.localRotation;
        float scale = isHovering ? hoverScale : idleScale;
        glowRect.localScale = new Vector3(
            targetRect.localScale.x * scale,
            targetRect.localScale.y * scale,
            targetRect.localScale.z);

        if (keepBehindTarget)
        {
            int targetIndex = transform.GetSiblingIndex();
            int glowIndex = Mathf.Max(0, targetIndex - 1);
            if (glowRect.GetSiblingIndex() != glowIndex)
                glowRect.SetSiblingIndex(glowIndex);
        }
    }

    private void SetGlowAlpha(float alpha, bool immediate)
    {
        targetAlpha = Mathf.Clamp01(alpha);

        if (immediate)
            currentAlpha = targetAlpha;

        if (glowImage == null)
            return;

        Color color = glowImage.color;
        color.a = currentAlpha;
        glowImage.color = color;
        glowImage.enabled = currentAlpha > 0.001f;
    }
}
