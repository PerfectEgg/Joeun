using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecognitionDecodeCell : MonoBehaviour, IDecodable, IPointerClickHandler
{
    const string RuntimeBitBackdropObjectName = "__DecodeBitBackdrop";
    const string RuntimeBitTextObjectName = "__DecodeBitText";
    const string RuntimeGridSlotShadeObjectName = "__DecodeGridSlotShade";
    const string DefaultBackdropSpritePath = "PatternPuzzle/arrow0";
    const string EmptySlotShadeSpritePath = "PatternPuzzle/decoding";

    static readonly Color DecodedUiTint = new Color(0.09f, 0.035f, 0.105f, 0.9f);
    static readonly Color DecodedSpriteTint = new Color(0.09f, 0.035f, 0.105f, 0.9f);
    static readonly Color DecodedBackdropColor = new Color(0.018f, 0.012f, 0.026f, 0.9f);
    static readonly Color DecodedTextColor = new Color(1f, 0.68f, 0.96f, 0.9f);

    [SerializeField] RecognitionDecodeAreaController controller;
    [SerializeField] bool bitValue;
    [SerializeField] Image uiImage;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Text uiText;
    [SerializeField] TMP_Text tmpText;
    [SerializeField] Sprite zeroSprite;
    [SerializeField] Sprite oneSprite;
    [SerializeField] GameObject zeroVisual;
    [SerializeField] GameObject oneVisual;
    [SerializeField] Collider2D decodeCollider;

    [FormerlySerializedAs("hiddenVisual")]
    [SerializeField, HideInInspector] GameObject legacyHiddenVisual;
    [FormerlySerializedAs("revealedVisual")]
    [SerializeField, HideInInspector] GameObject legacyRevealedVisual;
    [FormerlySerializedAs("expectedLetter")]
    [SerializeField, HideInInspector] char legacyExpectedLetter;
    [FormerlySerializedAs("decodeData")]
    [SerializeField, HideInInspector] DecodeData legacyDecodeData;

    Sprite originalUiSprite;
    Sprite originalRendererSprite;
    Color originalUiColor = Color.white;
    Color originalRendererColor = Color.white;
    Color originalUiTextColor = Color.white;
    Color originalTmpTextColor = Color.white;
    Color targetUiOriginalColor = Color.white;
    Color targetRendererOriginalColor = Color.white;
    bool targetUiOriginalEnabled = true;
    bool targetRendererOriginalEnabled = true;
    bool hasOriginalState;
    bool hasTargetOriginalState;
    bool decodeResultVisual;
    RectTransform targetSlotRect;
    GridSlot targetSlot;
    PatternPuzzleNodeVisual targetNodeVisual;
    Image targetUiImage;
    SpriteRenderer targetSpriteRenderer;
    Image targetEmptySlotShade;
    Image runtimeBackdrop;
    string RuntimeGridSlotShadeName => $"{RuntimeGridSlotShadeObjectName}_{GetInstanceID()}";

    public bool BitValue => bitValue;

    void Awake()
    {
        AutoWire();
        ApplyVisual(true);
    }

    void OnDisable()
    {
        RestoreGridTargetVisual();
    }

    void Reset()
    {
        AutoWire();
    }

    public void SetController(RecognitionDecodeAreaController owner)
    {
        controller = owner;
    }

    public void SetBitsVisible(bool visible)
    {
        ApplyVisual(visible);
    }

    public void SetDecodeResultVisual(bool active)
    {
        if (decodeResultVisual == active)
            return;

        decodeResultVisual = active;
        ApplyVisual(true);
    }

    public void SetDecodeAvailable(bool available)
    {
        if (decodeCollider != null)
            decodeCollider.enabled = available;
    }

    public bool TryDecoding(char keyWord)
    {
        return controller != null && controller.TryDecoding(keyWord);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null)
            controller.TryDecodeFromClick();
    }

    void AutoWire()
    {
        if (controller == null)
            controller = GetComponentInParent<RecognitionDecodeAreaController>();

        if (uiImage == null)
            uiImage = GetComponent<Image>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (uiText == null)
            uiText = GetComponentInChildren<Text>(true);

        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);

        EnsureRuntimeBackdrop();
        EnsureRuntimeBitText();

        if (decodeCollider == null)
            decodeCollider = GetComponent<Collider2D>();

        CaptureOriginalState();
    }

    void ApplyVisual(bool visible)
    {
        bool usesGridTarget = ResolveGridTarget();

        if (zeroVisual != null)
            zeroVisual.SetActive(!usesGridTarget && visible && decodeResultVisual && !bitValue);

        if (oneVisual != null)
            oneVisual.SetActive(!usesGridTarget && visible && decodeResultVisual && bitValue);

        if (uiImage != null)
        {
            uiImage.enabled = visible || IsAreaMarkerImage(uiImage);
            if (!usesGridTarget)
                uiImage.color = decodeResultVisual ? DecodedUiTint : originalUiColor;
        }

        if (runtimeBackdrop != null)
        {
            ApplyBackdropSprite();
            runtimeBackdrop.enabled = !usesGridTarget && visible && decodeResultVisual && uiImage == null && spriteRenderer == null;
            runtimeBackdrop.color = DecodedBackdropColor;
            runtimeBackdrop.transform.SetAsLastSibling();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
            if (!usesGridTarget)
                spriteRenderer.color = decodeResultVisual ? DecodedSpriteTint : originalRendererColor;
        }

        ApplyGridTargetVisual(usesGridTarget, visible && decodeResultVisual);
        SyncRuntimeBitTextParent(usesGridTarget ? targetSlotRect : transform as RectTransform);

        string valueText = bitValue ? "1" : "0";
        if (uiText != null)
        {
            uiText.text = valueText;
            uiText.enabled = visible && decodeResultVisual;
            uiText.color = decodeResultVisual ? DecodedTextColor : originalUiTextColor;
            uiText.transform.SetAsLastSibling();
        }

        if (tmpText != null)
        {
            tmpText.text = valueText;
            tmpText.enabled = visible && decodeResultVisual;
            tmpText.color = decodeResultVisual ? DecodedTextColor : originalTmpTextColor;
            tmpText.transform.SetAsLastSibling();
        }
    }

    bool ResolveGridTarget()
    {
        RectTransform selfRect = transform as RectTransform;
        PathPuzzleManager pathPuzzle = FindPathPuzzleManager();
        if (selfRect == null || pathPuzzle == null)
            return HasGridTarget();

        GridSlot[] slots = pathPuzzle.GetComponentsInChildren<GridSlot>(true);
        if (slots == null || slots.Length == 0)
            return HasGridTarget();

        Vector3 selfCenter = WorldCenter(selfRect);
        GridSlot closestSlot = null;
        float closestDistance = float.PositiveInfinity;

        foreach (GridSlot slot in slots)
        {
            if (slot == null || !(slot.transform is RectTransform slotRect))
                continue;

            float distance = (WorldCenter(slotRect) - selfCenter).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestSlot = slot;
        }

        if (closestSlot == null)
            return HasGridTarget();

        RectTransform slotTargetRect = closestSlot.transform as RectTransform;
        GridNode node = closestSlot.currentNode != null
            ? closestSlot.currentNode
            : closestSlot.GetComponentInChildren<GridNode>(true);

        PatternPuzzleNodeVisual nodeVisual = node != null ? node.GetComponent<PatternPuzzleNodeVisual>() : null;
        Image imageTarget = null;
        SpriteRenderer rendererTarget = null;

        if (node != null)
        {
            imageTarget = node.GetComponent<Image>();
            rendererTarget = node.GetComponent<SpriteRenderer>();
        }

        if (node != null && imageTarget == null)
            imageTarget = closestSlot.GetComponentInChildren<Image>(true);

        if (node != null && rendererTarget == null)
            rendererTarget = closestSlot.GetComponentInChildren<SpriteRenderer>(true);

        if (targetSlotRect == slotTargetRect
            && targetSlot == closestSlot
            && targetNodeVisual == nodeVisual
            && targetUiImage == imageTarget
            && targetSpriteRenderer == rendererTarget)
            return HasGridTarget();

        RestoreGridTargetVisual();

        targetSlotRect = slotTargetRect;
        targetSlot = closestSlot;
        targetNodeVisual = nodeVisual;
        targetUiImage = imageTarget;
        targetSpriteRenderer = rendererTarget;
        targetEmptySlotShade = null;
        hasTargetOriginalState = false;
        CaptureGridTargetState();

        return HasGridTarget();
    }

    PathPuzzleManager FindPathPuzzleManager()
    {
        PathPuzzleManager manager = GetComponentInParent<PathPuzzleManager>();
        if (manager != null)
            return manager;

        Transform cursor = transform;
        while (cursor != null)
        {
            manager = cursor.GetComponentInChildren<PathPuzzleManager>(true);
            if (manager != null)
                return manager;

            cursor = cursor.parent;
        }

        return null;
    }

    bool HasGridTarget()
    {
        return targetSlotRect != null && (targetNodeVisual != null || targetUiImage != null || targetSpriteRenderer != null || targetSlot != null);
    }

    void CaptureGridTargetState()
    {
        if (hasTargetOriginalState)
            return;

        if (targetUiImage != null)
        {
            targetUiOriginalColor = targetUiImage.color;
            targetUiOriginalEnabled = targetUiImage.enabled;
        }

        if (targetSpriteRenderer != null)
        {
            targetRendererOriginalColor = targetSpriteRenderer.color;
            targetRendererOriginalEnabled = targetSpriteRenderer.enabled;
        }

        hasTargetOriginalState = true;
    }

    void ApplyGridTargetVisual(bool usesGridTarget, bool active)
    {
        if (!usesGridTarget)
            return;

        CaptureGridTargetState();

        if (targetNodeVisual != null)
            targetNodeVisual.ClearColorOverride();

        if (targetUiImage != null)
            targetUiImage.color = targetUiOriginalColor;

        if (targetSpriteRenderer != null)
            targetSpriteRenderer.color = targetRendererOriginalColor;

        ApplyGridSlotShade(active);
    }

    void RestoreGridTargetVisual()
    {
        if (!hasTargetOriginalState)
            return;

        if (targetNodeVisual != null)
            targetNodeVisual.ClearColorOverride();

        if (targetUiImage != null)
            targetUiImage.color = targetUiOriginalColor;

        if (targetSpriteRenderer != null)
            targetSpriteRenderer.color = targetRendererOriginalColor;

        ApplyGridSlotShade(false);
    }

    void ApplyGridSlotShade(bool active)
    {
        bool needsShade = targetSlotRect != null;

        if (!needsShade)
        {
            if (targetEmptySlotShade != null)
                targetEmptySlotShade.enabled = false;

            return;
        }

        EnsureEmptySlotShade();

        if (targetEmptySlotShade == null)
            return;

        targetEmptySlotShade.enabled = active;
        targetEmptySlotShade.color = DecodedBackdropColor;
        targetEmptySlotShade.transform.SetAsLastSibling();
    }

    void EnsureEmptySlotShade()
    {
        if (targetSlotRect == null)
            return;

        if (targetEmptySlotShade != null)
            return;

        Transform existing = targetSlotRect.Find(RuntimeGridSlotShadeName);
        GameObject shadeObject = existing != null
            ? existing.gameObject
            : new GameObject(RuntimeGridSlotShadeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            shadeObject.transform.SetParent(targetSlotRect, false);

        RectTransform shadeRect = shadeObject.transform as RectTransform;
        Stretch(shadeRect);

        targetEmptySlotShade = shadeObject.GetComponent<Image>();
        targetEmptySlotShade.raycastTarget = false;
        targetEmptySlotShade.sprite = LoadSprite(EmptySlotShadeSpritePath);
        targetEmptySlotShade.type = Image.Type.Simple;
        targetEmptySlotShade.preserveAspect = false;
        targetEmptySlotShade.enabled = false;
    }

    void SyncRuntimeBitTextParent(RectTransform parent)
    {
        if (parent == null || uiText == null || uiText.name != RuntimeBitTextObjectName)
            return;

        RectTransform textRect = uiText.transform as RectTransform;
        if (textRect == null)
            return;

        if (textRect.parent != parent)
            textRect.SetParent(parent, false);

        Stretch(textRect);
        textRect.SetAsLastSibling();
    }

    static Vector3 WorldCenter(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    void EnsureRuntimeBackdrop()
    {
        if (!(transform is RectTransform))
            return;

        if (runtimeBackdrop != null)
            return;

        Transform existing = transform.Find(RuntimeBitBackdropObjectName);
        GameObject backdropObject = existing != null
            ? existing.gameObject
            : new GameObject(RuntimeBitBackdropObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            backdropObject.transform.SetParent(transform, false);

        RectTransform rect = backdropObject.transform as RectTransform;
        Stretch(rect);

        runtimeBackdrop = backdropObject.GetComponent<Image>();
        runtimeBackdrop.raycastTarget = false;
        runtimeBackdrop.enabled = false;
        runtimeBackdrop.color = DecodedBackdropColor;
        runtimeBackdrop.type = Image.Type.Simple;
        runtimeBackdrop.preserveAspect = false;
    }

    void ApplyBackdropSprite()
    {
        if (runtimeBackdrop == null)
            return;

        Sprite sprite = null;
        if (hasOriginalState && originalUiSprite != null)
            sprite = originalUiSprite;

        if (sprite == null && uiImage != null && uiImage.sprite != null)
            sprite = uiImage.sprite;

        if (sprite == null && hasOriginalState && originalRendererSprite != null)
            sprite = originalRendererSprite;

        if (sprite == null)
            sprite = LoadSprite(DefaultBackdropSpritePath);

        runtimeBackdrop.sprite = sprite;
    }

    void EnsureRuntimeBitText()
    {
        if (uiText != null || tmpText != null || !(transform is RectTransform))
            return;

        Transform existing = transform.Find(RuntimeBitTextObjectName);
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(RuntimeBitTextObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));

        if (existing == null)
            textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.transform as RectTransform;
        Stretch(rect);
        textObject.transform.SetAsLastSibling();

        uiText = textObject.GetComponent<Text>();
        uiText.raycastTarget = false;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontStyle = FontStyle.Bold;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = 10;
        uiText.resizeTextMaxSize = 42;
        uiText.supportRichText = false;
        uiText.enabled = false;

        if (uiText.font == null)
        {
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiText.font == null)
                uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    void CaptureOriginalState()
    {
        if (hasOriginalState)
            return;

        if (uiImage != null)
        {
            originalUiSprite = uiImage.sprite;
            originalUiColor = uiImage.color;
        }

        if (spriteRenderer != null)
        {
            originalRendererSprite = spriteRenderer.sprite;
            originalRendererColor = spriteRenderer.color;
        }

        if (uiText != null)
            originalUiTextColor = uiText.color;

        if (tmpText != null)
            originalTmpTextColor = tmpText.color;

        hasOriginalState = true;
    }

    bool IsAreaMarkerImage(Image image)
    {
        return image != null && image.color.a <= 0.08f;
    }
}
