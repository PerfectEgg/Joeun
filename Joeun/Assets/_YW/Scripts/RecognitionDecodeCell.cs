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

    static readonly Color DecodedUiTint = new Color(0.12f, 0.17f, 0.18f, 0.82f);
    static readonly Color DecodedSpriteTint = new Color(0.12f, 0.17f, 0.18f, 0.82f);
    static readonly Color DecodedBackdropColor = new Color(0.04f, 0.075f, 0.08f, 0.86f);
    static readonly Color DecodedTextColor = new Color(0.5f, 0.9f, 0.86f, 0.82f);

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
    bool hasOriginalState;
    bool decodeResultVisual;
    Image runtimeBackdrop;

    public bool BitValue => bitValue;

    void Awake()
    {
        AutoWire();
        ApplyVisual(true);
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
        if (zeroVisual != null)
            zeroVisual.SetActive(visible && decodeResultVisual && !bitValue);

        if (oneVisual != null)
            oneVisual.SetActive(visible && decodeResultVisual && bitValue);

        Sprite sprite = decodeResultVisual ? bitValue ? oneSprite : zeroSprite : null;

        if (uiImage != null)
        {
            if (sprite != null)
                uiImage.sprite = sprite;
            else if (hasOriginalState)
                uiImage.sprite = originalUiSprite;

            uiImage.enabled = visible;
            uiImage.color = decodeResultVisual ? DecodedUiTint : originalUiColor;
        }

        if (runtimeBackdrop != null)
        {
            runtimeBackdrop.enabled = visible && decodeResultVisual;
            runtimeBackdrop.color = DecodedBackdropColor;
            runtimeBackdrop.transform.SetAsLastSibling();
        }

        if (spriteRenderer != null)
        {
            if (sprite != null)
                spriteRenderer.sprite = sprite;
            else if (hasOriginalState)
                spriteRenderer.sprite = originalRendererSprite;

            spriteRenderer.enabled = visible;
            spriteRenderer.color = decodeResultVisual ? DecodedSpriteTint : originalRendererColor;
        }

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
}
