using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecognitionDecodeAreaController : MonoBehaviour, IDecodable, IPointerClickHandler
{
    const string RuntimeAreaShadeObjectName = "__RecognitionDecodeAreaShade";

    static readonly Color DecodedAreaShadeColor = new Color(0.012f, 0.016f, 0.022f, 0.88f);

    [Header("Decode Gate")]
    [SerializeField] bool decodeReady;
    [SerializeField] bool requireDecodeMode = true;
    [SerializeField] bool grantDecodeSkillOnReady = true;
    [SerializeField] bool selectDecodeSkillOnReady;
    [SerializeField] bool resetOnEnable = true;

    [Header("Answer")]
    [SerializeField] DecodeData answerData;
    [SerializeField] char expectedLetter;
    [SerializeField] string expectedText;
    [SerializeField] bool inferLetterFromObjectName = true;

    [Header("2x2 Bit Pattern")]
    [FormerlySerializedAs("cells")]
    [SerializeField] RecognitionDecodeCell[] bitCells = new RecognitionDecodeCell[4];
    [SerializeField] bool hideBitsWhenDecoded = true;
    [SerializeField] bool autoDecodeOnAreaClick;

    [Header("Reveal Visual")]
    [SerializeField] GameObject hiddenVisual;
    [SerializeField] GameObject revealedVisual;
    [SerializeField] Image revealImage;
    [SerializeField] SpriteRenderer revealSpriteRenderer;
    [SerializeField] Text revealText;
    [SerializeField] TMP_Text revealTmpText;

    [Header("Highlight")]
    [SerializeField] SkillHighlightTarget decodeHighlight;
    [SerializeField] Image highlightImage;
    [SerializeField] RecognitionDecodeScanVisualMode scanVisualMode = RecognitionDecodeScanVisualMode.ProceduralLine;
    [SerializeField] UnityEngine.Object scanImageAsset;
    [SerializeField, HideInInspector] RectTransform highlightRect;
    [SerializeField] Collider2D areaCollider;
    [SerializeField, HideInInspector] RecognitionDecodeScanEffect scanEffect;
    [SerializeField, HideInInspector] Image areaShadeImage;
    float decodeHighlightCornerRadius = -1f;

    [Header("Events")]
    [SerializeField] UnityEvent onDecodeReady;
    [SerializeField] UnityEvent onDecoded;

    bool decoded;
    bool isDecoding;
    Graphic inputRaycastGraphic;
    Coroutine decodeRoutine;

    public bool DecodeReady => decodeReady;
    public bool IsDecoded => decoded;
    public bool AutoDecodeOnAreaClick => autoDecodeOnAreaClick;
    public char ExpectedLetter => FirstLetter(ExpectedText);
    public string ExpectedText => ResolveExpectedText();
    public event System.Action<RecognitionDecodeAreaController> Decoded;

    public bool CanDecode
    {
        get
        {
            if (!decodeReady || decoded || isDecoding)
                return false;

            if (SkillInteractionLock.IsLocked)
                return false;

            return !requireDecodeMode || SkillIconModeView.CurrentMode == SkillModeType.Decode;
        }
    }

    void Awake()
    {
        AutoWire();
        ConfigureHighlight();
        ApplyVisual();
        ApplyDecodeAvailability();
    }

    void Reset()
    {
        AutoWire();
    }

    void OnValidate()
    {
        if (highlightImage != null)
            highlightRect = highlightImage.rectTransform;
    }

    void OnEnable()
    {
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;

        if (resetOnEnable)
            ResetDecode();
        else
            ApplyDecodeAvailability();
    }

    void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;

        if (decodeRoutine != null)
        {
            StopCoroutine(decodeRoutine);
            decodeRoutine = null;
        }

        isDecoding = false;
    }

    public void MarkDecodeReady()
    {
        SetDecodeReady(true);
    }

    public void SetExpectedText(string text)
    {
        expectedText = string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().ToUpperInvariant();

        if (expectedText.Length == 1)
            expectedLetter = expectedText[0];

        ApplyVisual();
        ApplyDecodeAvailability();
    }

    public void SetDecodeReady(bool ready)
    {
        if (decodeReady == ready)
        {
            ApplyDecodeAvailability();
            return;
        }

        decodeReady = ready;

        if (decodeReady)
        {
            if (grantDecodeSkillOnReady)
                SkillModeStageRules.Grant(SkillModeType.Decode);

            if (selectDecodeSkillOnReady)
                SkillIconModeView.SelectMode(SkillModeType.Decode);

            onDecodeReady?.Invoke();
        }

        ApplyDecodeAvailability();
    }

    public void ClearDecodeHighlight()
    {
        if (scanEffect != null)
            scanEffect.Hide();

        if (decodeHighlight != null)
        {
            decodeHighlight.ForceClear();
            decodeHighlight.enabled = false;
        }
    }

    public void ResetDecode()
    {
        if (decodeRoutine != null)
        {
            StopCoroutine(decodeRoutine);
            decodeRoutine = null;
        }

        decoded = false;
        isDecoding = false;

        if (scanEffect != null)
            scanEffect.Hide();

        ApplyVisual();
        ApplyDecodeAvailability();
    }

    public bool TryDecoding(char keyWord)
    {
        if (!CanDecode)
            return false;

        if (Normalize(keyWord) != Normalize(ExpectedLetter))
            return false;

        Decode();
        return true;
    }

    public void Decode()
    {
        if (decoded || isDecoding)
            return;

        decodeRoutine = StartCoroutine(DecodeRoutine());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryDecodeFromClick();
    }

    void OnMouseDown()
    {
        TryDecodeFromClick();
    }

    public void SetAutoDecodeOnAreaClick(bool enabled)
    {
        autoDecodeOnAreaClick = enabled;
    }

    public void TryDecodeFromClick()
    {
        if (CanDecode)
            Decode();
    }

    void AutoWire()
    {
        bool usesLetterBankVisual = GetComponent<LetterBankLetterView>() != null;

        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (scanEffect == null)
            scanEffect = GetComponent<RecognitionDecodeScanEffect>();

        Image selfImage = GetComponent<Image>();
        if ((usesLetterBankVisual || IsAreaMarkerImage(selfImage)) && revealImage == selfImage)
            revealImage = null;

        if (!usesLetterBankVisual && revealImage == null)
        {
            if (selfImage != null && selfImage.sprite != null && !IsAreaMarkerImage(selfImage))
                revealImage = selfImage;
        }

        if (revealSpriteRenderer == null)
            revealSpriteRenderer = GetComponent<SpriteRenderer>();

        SanitizeRevealTextRefs();

        if (usesLetterBankVisual && revealText == GetComponent<Text>())
            revealText = null;

        if (usesLetterBankVisual && revealTmpText == GetComponent<TMP_Text>())
            revealTmpText = null;

        if (!usesLetterBankVisual && revealText == null)
            revealText = GetComponent<Text>();

        if (!usesLetterBankVisual && revealTmpText == null)
            revealTmpText = GetComponent<TMP_Text>();

        bool hasCell = false;
        foreach (RecognitionDecodeCell cell in bitCells)
        {
            if (cell != null)
            {
                hasCell = true;
                break;
            }
        }

        if (!hasCell)
            bitCells = GetComponentsInChildren<RecognitionDecodeCell>(true);

        foreach (RecognitionDecodeCell cell in bitCells)
        {
            if (cell != null)
                cell.SetController(this);
        }

        EnsureInputRaycastGraphic();
        EnsureScanEffect();
    }

    void ConfigureHighlight()
    {
        bool usesLetterBankVisual = GetComponent<LetterBankLetterView>() != null;
        RectTransform targetRect = ResolveHighlightRect();

        if (decodeHighlight == null)
            decodeHighlight = GetComponentInChildren<SkillHighlightTarget>(true);

        if (decodeHighlight == null)
            decodeHighlight = gameObject.AddComponent<SkillHighlightTarget>();

        decodeHighlight.SetFrameCornerRadius(decodeHighlightCornerRadius);
        decodeHighlight.Configure(false, false, true, targetRect);
        decodeHighlight.SetStableFrame(usesLetterBankVisual);

        if (scanEffect != null)
            ApplyScanEffectArea();

        EnsureAreaShade();
    }

    RectTransform FindHighlightAreaRect()
    {
        Image image = FindHighlightAreaImage();
        return image != null ? image.rectTransform : null;
    }

    Image FindHighlightAreaImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        Image namedFallback = null;

        foreach (Image image in images)
        {
            if (image == null)
                continue;

            if (image.transform == transform)
            {
                if (IsAreaMarkerImage(image))
                    return image;

                continue;
            }

            if (image == revealImage)
                continue;

            if (image.GetComponentInParent<RecognitionDecodeCell>() != null)
                continue;

            if (image.name.StartsWith("__RecognitionDecode", System.StringComparison.Ordinal))
                continue;

            string normalizedName = image.name.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
            bool explicitArea =
                normalizedName.Contains("highlight")
                || normalizedName.Contains("hitarea")
                || normalizedName.Contains("decodearea")
                || normalizedName.Contains("area");

            if (!explicitArea)
                continue;

            if (IsAreaMarkerImage(image))
                return image;

            if (namedFallback == null)
                namedFallback = image;
        }

        return namedFallback;
    }

    RectTransform ResolveHighlightRect()
    {
        if (highlightImage != null)
        {
            highlightRect = highlightImage.rectTransform;
            return highlightRect;
        }

        if (highlightRect != null)
            return highlightRect;

        highlightImage = FindHighlightAreaImage();
        if (highlightImage != null)
        {
            highlightRect = highlightImage.rectTransform;
            return highlightRect;
        }

        highlightRect = transform as RectTransform;
        return highlightRect;
    }

    void ApplyScanEffectArea()
    {
        if (scanEffect == null)
            return;

        scanEffect.SetScanVisualMode(scanVisualMode);
        scanEffect.SetFillScanAsset(scanImageAsset);

        if (highlightImage != null)
        {
            scanEffect.SetScanImage(highlightImage);
            return;
        }

        RectTransform targetRect = ResolveHighlightRect();
        if (targetRect != null)
            scanEffect.SetScanArea(targetRect);
    }

    void ApplyVisual()
    {
        bool showDecodeResult = decoded || isDecoding;
        SetAreaShadeVisible(showDecodeResult);

        if (hiddenVisual != null)
            hiddenVisual.SetActive(!decoded);

        if (revealedVisual != null)
            revealedVisual.SetActive(decoded);

        foreach (RecognitionDecodeCell cell in bitCells)
        {
            if (cell == null)
                continue;

            cell.SetDecodeResultVisual(showDecodeResult);
            cell.SetBitsVisible(showDecodeResult || !decoded || !hideBitsWhenDecoded);
            cell.SetDecodeAvailable(CanDecode);
        }

        Sprite icon = answerData != null ? answerData.decodeIcon : null;

        if (revealImage != null)
        {
            if (icon != null)
                revealImage.sprite = icon;

            if (IsAreaMarkerImage(revealImage))
                revealImage.enabled = true;
            else
                revealImage.enabled = decoded && revealImage.sprite != null;
        }

        if (revealSpriteRenderer != null)
        {
            if (icon != null)
                revealSpriteRenderer.sprite = icon;

            revealSpriteRenderer.enabled = decoded && revealSpriteRenderer.sprite != null;
        }

        string text = ExpectedText;
        if (revealText != null)
        {
            revealText.text = text;
            revealText.enabled = decoded;
        }

        if (revealTmpText != null)
        {
            revealTmpText.text = text;
            revealTmpText.enabled = decoded;
        }
    }

    void ApplyDecodeAvailability()
    {
        bool canDecode = CanDecode;

        if (areaCollider != null)
            areaCollider.enabled = canDecode;

        if (inputRaycastGraphic != null)
        {
            if (IsAreaMarkerImage(inputRaycastGraphic as Image))
                inputRaycastGraphic.enabled = true;

            inputRaycastGraphic.raycastTarget = canDecode;
        }

        foreach (RecognitionDecodeCell cell in bitCells)
        {
            if (cell != null)
                cell.SetDecodeAvailable(canDecode);
        }

        if (decodeHighlight != null)
        {
            bool showHighlight = decodeReady && !decoded && !isDecoding;
            decodeHighlight.enabled = showHighlight;

            if (!showHighlight)
                decodeHighlight.ForceClear();
        }
    }

    void SanitizeRevealTextRefs()
    {
        if (revealText != null && revealText.GetComponentInParent<RecognitionDecodeCell>() != null)
            revealText = null;

        if (revealTmpText != null && revealTmpText.GetComponentInParent<RecognitionDecodeCell>() != null)
            revealTmpText = null;
    }

    System.Collections.IEnumerator DecodeRoutine()
    {
        isDecoding = true;
        ApplyVisual();
        ApplyDecodeAvailability();

        EnsureScanEffect();
        if (scanEffect != null)
            yield return scanEffect.Play(ExpectedText);

        decoded = true;
        isDecoding = false;
        decodeRoutine = null;

        ApplyVisual();
        ApplyDecodeAvailability();
        Decoded?.Invoke(this);
        onDecoded?.Invoke();
    }

    void HandleSkillModeChanged(SkillModeType mode)
    {
        ApplyDecodeAvailability();
    }

    void HandleInteractionLockChanged(bool locked)
    {
        ApplyDecodeAvailability();
    }

    void EnsureInputRaycastGraphic()
    {
        if (!(transform is RectTransform))
            return;

        Graphic graphic = GetComponent<Graphic>();
        if (graphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            inputRaycastGraphic = image;
            return;
        }

        inputRaycastGraphic = graphic;
    }

    bool IsAreaMarkerImage(Image image)
    {
        if (image == null)
            return false;

        if (image.color.a <= 0.08f)
            return true;

        string normalizedName = image.name.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        return normalizedName.Contains("highlight")
            || normalizedName.Contains("hitarea")
            || normalizedName.Contains("decodearea")
            || normalizedName.Contains("area");
    }

    void EnsureScanEffect()
    {
        if (!(transform is RectTransform))
            return;

        if (scanEffect == null)
            scanEffect = GetComponent<RecognitionDecodeScanEffect>();

        if (scanEffect == null)
            scanEffect = gameObject.AddComponent<RecognitionDecodeScanEffect>();

        ApplyScanEffectArea();

        scanEffect.Prepare();
    }

    void EnsureAreaShade()
    {
        RectTransform targetRect = ResolveHighlightRect();
        if (targetRect == null)
            return;

        if (areaShadeImage != null && areaShadeImage.transform.parent == targetRect)
            return;

        Transform existing = targetRect.Find(RuntimeAreaShadeObjectName);
        GameObject shadeObject = existing != null
            ? existing.gameObject
            : new GameObject(RuntimeAreaShadeObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            shadeObject.transform.SetParent(targetRect, false);

        RectTransform shadeRect = shadeObject.transform as RectTransform;
        Stretch(shadeRect);

        areaShadeImage = shadeObject.GetComponent<Image>();
        areaShadeImage.raycastTarget = false;
        areaShadeImage.maskable = false;
        areaShadeImage.color = DecodedAreaShadeColor;
        areaShadeImage.sprite = highlightImage != null ? highlightImage.sprite : null;
        areaShadeImage.type = highlightImage != null ? highlightImage.type : Image.Type.Simple;
        areaShadeImage.preserveAspect = false;
        areaShadeImage.enabled = false;
        areaShadeImage.transform.SetAsFirstSibling();
    }

    void SetAreaShadeVisible(bool visible)
    {
        EnsureAreaShade();

        if (areaShadeImage == null)
            return;

        areaShadeImage.enabled = visible;
        areaShadeImage.color = DecodedAreaShadeColor;
        areaShadeImage.transform.SetAsFirstSibling();
    }

    static void Stretch(RectTransform rect)
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

    string ResolveExpectedText()
    {
        if (answerData != null && answerData.decodeLetter != '\0')
            return answerData.decodeLetter.ToString();

        if (!string.IsNullOrWhiteSpace(expectedText))
            return expectedText.Trim().ToUpperInvariant();

        if (expectedLetter != '\0')
            return expectedLetter.ToString();

        if (!inferLetterFromObjectName)
            return string.Empty;

        string objectName = name;
        int separator = objectName.LastIndexOf('_');
        if (separator >= 0 && separator + 1 < objectName.Length)
            return objectName.Substring(separator + 1).Trim().ToUpperInvariant();

        return string.Empty;
    }

    static char FirstLetter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return '\0';

        return Normalize(value.Trim()[0]);
    }

    static char Normalize(char value)
    {
        return char.ToUpperInvariant(value);
    }
}
