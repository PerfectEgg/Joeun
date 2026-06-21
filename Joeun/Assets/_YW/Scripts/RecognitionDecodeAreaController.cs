using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecognitionDecodeAreaController : MonoBehaviour, IDecodable, IPointerClickHandler
{
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
    [SerializeField] RectTransform highlightRect;
    [SerializeField] Collider2D areaCollider;
    [SerializeField, HideInInspector] RecognitionDecodeScanEffect scanEffect;

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

        if (usesLetterBankVisual && revealImage == GetComponent<Image>())
            revealImage = null;

        if (!usesLetterBankVisual && revealImage == null)
        {
            Image image = GetComponent<Image>();
            if (image != null && image.sprite != null)
                revealImage = image;
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

        if (highlightRect == null)
            highlightRect = transform as RectTransform;

        if (decodeHighlight == null)
            decodeHighlight = GetComponentInChildren<SkillHighlightTarget>(true);

        if (decodeHighlight == null)
            decodeHighlight = gameObject.AddComponent<SkillHighlightTarget>();

        decodeHighlight.Configure(false, false, true, highlightRect);
        decodeHighlight.SetStableFrame(usesLetterBankVisual);
    }

    void ApplyVisual()
    {
        if (hiddenVisual != null)
            hiddenVisual.SetActive(!decoded);

        if (revealedVisual != null)
            revealedVisual.SetActive(decoded);

        foreach (RecognitionDecodeCell cell in bitCells)
        {
            if (cell == null)
                continue;

            bool showDecodeResult = decoded || isDecoding;
            cell.SetDecodeResultVisual(showDecodeResult);
            cell.SetBitsVisible(showDecodeResult || !decoded || !hideBitsWhenDecoded);
            cell.SetDecodeAvailable(CanDecode);
        }

        Sprite icon = answerData != null ? answerData.decodeIcon : null;

        if (revealImage != null)
        {
            if (icon != null)
                revealImage.sprite = icon;

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
            inputRaycastGraphic.raycastTarget = canDecode;

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

    void EnsureScanEffect()
    {
        if (!(transform is RectTransform))
            return;

        if (scanEffect == null)
            scanEffect = GetComponent<RecognitionDecodeScanEffect>();

        if (scanEffect == null)
            scanEffect = gameObject.AddComponent<RecognitionDecodeScanEffect>();

        scanEffect.Prepare();
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
