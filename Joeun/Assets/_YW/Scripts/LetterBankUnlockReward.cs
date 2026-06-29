using UnityEngine;

[DisallowMultipleComponent]
public sealed class LetterBankUnlockReward : MonoBehaviour
{
    [SerializeField] private LetterBankController bank;
    [SerializeField] private RecognitionDecodeAreaController decodeArea;
    [SerializeField] private string lettersToUnlock;
    [SerializeField] private bool useDecodeAreaExpectedLetter = true;
    [SerializeField] private bool unlockOnlyOnce = true;

    private bool unlocked;

    private void Awake()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        AutoWire();

        if (decodeArea != null)
            decodeArea.Decoded += HandleDecoded;
    }

    private void OnDisable()
    {
        if (decodeArea != null)
            decodeArea.Decoded -= HandleDecoded;
    }

    public void Unlock()
    {
        if (unlockOnlyOnce && unlocked)
            return;

        AutoWire();

        if (bank == null)
        {
            Debug.LogWarning($"{nameof(LetterBankUnlockReward)} needs a LetterBankController.", this);
            return;
        }

        string letters = ResolveLetters();
        if (string.IsNullOrEmpty(letters))
        {
            Debug.LogWarning($"{nameof(LetterBankUnlockReward)} has no letter to unlock.", this);
            return;
        }

        bank.UnlockLetters(letters);
        unlocked = true;
    }

    public void SetLettersToUnlock(string letters)
    {
        lettersToUnlock = letters;
    }

    void HandleDecoded(RecognitionDecodeAreaController area)
    {
        Unlock();
    }

    void AutoWire()
    {
        if (decodeArea == null)
            decodeArea = GetComponent<RecognitionDecodeAreaController>();

        if (decodeArea == null)
            decodeArea = GetComponentInParent<RecognitionDecodeAreaController>();

        if (bank == null)
            bank = GetComponentInParent<LetterBankController>();

        if (bank == null)
            bank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);
    }

    string ResolveLetters()
    {
        if (!string.IsNullOrWhiteSpace(lettersToUnlock))
            return lettersToUnlock.ToUpperInvariant();

        if (!useDecodeAreaExpectedLetter || decodeArea == null || string.IsNullOrWhiteSpace(decodeArea.ExpectedText))
            return string.Empty;

        return decodeArea.ExpectedText;
    }
}
