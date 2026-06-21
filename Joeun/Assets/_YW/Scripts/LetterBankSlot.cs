using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LetterBankSlot : MonoBehaviour
{
    static readonly Color OffPanelColor = new Color(0.34f, 0.34f, 0.34f, 0.62f);
    static readonly Color OnPanelColor = new Color(1f, 1f, 1f, 1f);
    static readonly Color OffTextColor = new Color(0.62f, 0.62f, 0.62f, 0.52f);
    static readonly Color OnTextColor = new Color(1f, 1f, 1f, 1f);
    static readonly Color PulseColor = new Color(0.82f, 0.88f, 0.9f, 1f);

    [SerializeField] private char letter;
    [SerializeField] private Graphic panelGraphic;
    [SerializeField] private Text uiText;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button button;
    [SerializeField] private GameObject offVisual;
    [SerializeField] private GameObject onVisual;
    [SerializeField] private bool clickableWhenUnlocked;
    [SerializeField] private UnityEvent onSelected;

    private LetterBankController bank;
    private Coroutine feedbackRoutine;
    private bool unlocked;

    public char Letter => Normalize(letter);
    public bool IsUnlocked => unlocked;

    private void Awake()
    {
        AutoWire();
        Apply(true);
    }

    private void Reset()
    {
        AutoWire();
        TryInferLetterFromName();
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void OnDisable()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }
    }

    public void SetBank(LetterBankController owner)
    {
        bank = owner;
    }

    public void SetLetter(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        letter = Normalize(value[0]);
        Apply(true);
    }

    public void SetUnlocked(bool value, bool instant = false)
    {
        bool changed = unlocked != value;
        unlocked = value;
        Apply(instant);

        if (changed && unlocked && !instant)
            PlayUnlockFeedback();
    }

    public void Select()
    {
        if (!unlocked || !clickableWhenUnlocked)
            return;

        onSelected?.Invoke();
    }

    void AutoWire()
    {
        if (panelGraphic == null)
            panelGraphic = GetComponent<Graphic>();

        if (uiText == null)
            uiText = GetComponentInChildren<Text>(true);

        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }
    }

    void Apply(bool instant)
    {
        AutoWire();

        string text = Letter == '\0' ? string.Empty : Letter.ToString();

        if (uiText != null)
        {
            uiText.text = text;
            uiText.color = unlocked ? OnTextColor : OffTextColor;
        }

        if (tmpText != null)
        {
            tmpText.text = text;
            tmpText.color = unlocked ? OnTextColor : OffTextColor;
        }

        if (panelGraphic != null)
            panelGraphic.color = unlocked ? OnPanelColor : OffPanelColor;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = unlocked ? 1f : 0.62f;
            canvasGroup.interactable = unlocked && clickableWhenUnlocked;
            canvasGroup.blocksRaycasts = unlocked && clickableWhenUnlocked;
        }

        if (button != null)
            button.interactable = unlocked && clickableWhenUnlocked;

        if (offVisual != null)
            offVisual.SetActive(!unlocked);

        if (onVisual != null)
            onVisual.SetActive(unlocked);

        if (instant)
            transform.localScale = Vector3.one;
    }

    void PlayUnlockFeedback()
    {
        if (!isActiveAndEnabled)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(UnlockFeedbackRoutine());
    }

    IEnumerator UnlockFeedbackRoutine()
    {
        const float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            if (panelGraphic != null)
                panelGraphic.color = Color.Lerp(OnPanelColor, PulseColor, pulse);

            transform.localScale = Vector3.one * (1f + pulse * 0.08f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        Apply(true);
        feedbackRoutine = null;
    }

    void TryInferLetterFromName()
    {
        if (Letter != '\0')
            return;

        foreach (char value in name)
        {
            if (char.IsLetter(value))
            {
                letter = char.ToUpperInvariant(value);
                return;
            }
        }
    }

    static char Normalize(char value)
    {
        if (!char.IsLetter(value))
            return '\0';

        return char.ToUpperInvariant(value);
    }
}
