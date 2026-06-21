using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LetterBankLetterView : MonoBehaviour
{
    private const string LetterResourceFolder = "DecodeLetters";

    [SerializeField] private LetterBankController bank;
    [SerializeField] private string letter;
    [SerializeField] private Image image;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Text uiText;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private RecognitionDecodeAreaController decodeArea;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [SerializeField] private bool disableDecodeAreaWhenUnlocked = true;

    public char Letter => Normalize(string.IsNullOrWhiteSpace(letter) ? '\0' : letter[0]);

    private void Awake()
    {
        AutoWire();
        Apply();
    }

    private void Reset()
    {
        AutoWire();
        InferLetterFromName();
    }

    private void OnEnable()
    {
        AutoWire();
        Subscribe();
        Apply();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetLetter(string value)
    {
        letter = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

        Apply();
    }

    public void Refresh()
    {
        AutoWire();
        Apply();
    }

    public bool TryMarkDecodeReady()
    {
        AutoWire();

        char normalized = Letter;
        if (normalized == '\0')
            return false;

        if (decodeArea == null)
            decodeArea = gameObject.AddComponent<RecognitionDecodeAreaController>();

        decodeArea.SetExpectedText(normalized.ToString());
        decodeArea.MarkDecodeReady();

        LetterBankUnlockReward reward = GetComponent<LetterBankUnlockReward>();
        if (reward == null)
            reward = gameObject.AddComponent<LetterBankUnlockReward>();

        reward.SetLettersToUnlock(normalized.ToString());
        return true;
    }

    private void AutoWire()
    {
        if (bank == null)
            bank = GetComponentInParent<LetterBankController>();

        if (bank == null)
            bank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);

        if (image == null)
            image = GetComponent<Image>();

        if (uiText == null)
            uiText = GetComponent<Text>();

        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        if (decodeArea == null)
            decodeArea = GetComponent<RecognitionDecodeAreaController>();

        InferLetterFromName();
    }

    private void Subscribe()
    {
        if (bank == null)
            return;

        bank.Changed -= Apply;
        bank.LetterUnlocked -= HandleLetterUnlocked;
        bank.Changed += Apply;
        bank.LetterUnlocked += HandleLetterUnlocked;
    }

    private void Unsubscribe()
    {
        if (bank == null)
            return;

        bank.Changed -= Apply;
        bank.LetterUnlocked -= HandleLetterUnlocked;
    }

    private void HandleLetterUnlocked(char unlockedLetter)
    {
        if (Normalize(unlockedLetter) == Letter)
            Apply();
    }

    private void Apply()
    {
        char normalized = Letter;
        bool unlocked = normalized != '\0' && bank != null && bank.IsUnlocked(normalized);

        ApplyObjects(unlocked);
        ApplyImage(unlocked);
        ApplyText(normalized, unlocked);

        if (unlocked && disableDecodeAreaWhenUnlocked && decodeArea != null)
        {
            decodeArea.SetDecodeReady(false);
            decodeArea.ClearDecodeHighlight();
        }
    }

    private void ApplyObjects(bool unlocked)
    {
        if (lockedVisual != null)
            lockedVisual.SetActive(!unlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(unlocked);
    }

    private void ApplyImage(bool unlocked)
    {
        if (image == null)
            return;

        ResolveSpritesIfNeeded();

        Sprite sprite = unlocked ? unlockedSprite : lockedSprite;
        if (sprite != null)
            image.sprite = sprite;
    }

    private void ResolveSpritesIfNeeded()
    {
        char normalized = Letter;
        if (normalized == '\0')
            return;

        if (lockedSprite == null)
            lockedSprite = LoadLetterSprite("letter_censored");

        if (unlockedSprite == null)
            unlockedSprite = LoadLetterSprite($"letter_{normalized}");
    }

    private static Sprite LoadLetterSprite(string assetName)
    {
        Sprite sprite = Resources.Load<Sprite>($"{LetterResourceFolder}/{assetName}");
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>($"{LetterResourceFolder}/{assetName}");
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    private void ApplyText(char normalized, bool unlocked)
    {
        string value = unlocked && normalized != '\0'
            ? normalized.ToString()
            : "#";

        if (uiText != null)
            uiText.text = value;

        if (tmpText != null)
            tmpText.text = value;
    }

    private void InferLetterFromName()
    {
        if (Letter != '\0')
            return;

        string[] tokens = name.Split('_', '-', ' ');
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            string token = tokens[i];
            if (token.Length != 1 || !char.IsLetter(token[0]))
                continue;

            letter = char.ToUpperInvariant(token[0]).ToString();
            return;
        }
    }

    private static char Normalize(char value)
    {
        return char.IsLetter(value) ? char.ToUpperInvariant(value) : '\0';
    }
}
