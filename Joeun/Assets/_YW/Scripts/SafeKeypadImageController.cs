using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SafeKeypadImageController : MonoBehaviour
{
    private const int CodeLength = 4;

    [Header("Code")]
    [SerializeField] private string correctCode = "1234";

    [Header("Display")]
    [SerializeField] private GameObject[] displaySlots = new GameObject[CodeLength];

    [Header("Key Sprites")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite hashSprite;

    [Header("Feedback")]
    [SerializeField] private GameObject failFeedbackObject;
    [SerializeField] private GameObject successFeedbackObject;
    [SerializeField] private float lastKeyRevealDelay = 0.18f;
    [SerializeField] private float failFeedbackDuration = 0.6f;
    [SerializeField] private float successFeedbackDuration = 0.6f;

    [Header("Safe State")]
    [SerializeField] private GameObject safeClosedObject;
    [SerializeField] private GameObject safeOpenObject;
    [SerializeField] private UnityEvent onUnlocked;
    [SerializeField] private UnityEvent onFailed;

    private readonly SlotTarget[] slotTargets = new SlotTarget[CodeLength];
    private string currentInput = "";
    private Coroutine validationRoutine;
    private bool isBusy;
    private bool isUnlocked;

    private void Awake()
    {
        CacheSlotTargets();
        SetFeedbackActive(failFeedbackObject, false);
        SetFeedbackActive(successFeedbackObject, false);
        ClearInput();
    }

    private void OnValidate()
    {
        if (displaySlots == null || displaySlots.Length != CodeLength)
            System.Array.Resize(ref displaySlots, CodeLength);

        if (digitSprites == null || digitSprites.Length != 10)
            System.Array.Resize(ref digitSprites, 10);

        lastKeyRevealDelay = Mathf.Max(0f, lastKeyRevealDelay);
        failFeedbackDuration = Mathf.Max(0f, failFeedbackDuration);
        successFeedbackDuration = Mathf.Max(0f, successFeedbackDuration);
    }

    public void PressKey(string key)
    {
        if (isBusy || isUnlocked)
            return;

        key = NormalizeKey(key);
        if (string.IsNullOrEmpty(key))
            return;

        if (currentInput.Length >= CodeLength)
            return;

        currentInput += key;
        UpdateDisplay();

        if (currentInput.Length == CodeLength)
            validationRoutine = StartCoroutine(ValidateInput());
    }

    public void Press0() => PressKey("0");
    public void Press1() => PressKey("1");
    public void Press2() => PressKey("2");
    public void Press3() => PressKey("3");
    public void Press4() => PressKey("4");
    public void Press5() => PressKey("5");
    public void Press6() => PressKey("6");
    public void Press7() => PressKey("7");
    public void Press8() => PressKey("8");
    public void Press9() => PressKey("9");
    public void PressStar() => PressKey("*");
    public void PressHash() => PressKey("#");

    public void ResetInput()
    {
        if (validationRoutine != null)
        {
            StopCoroutine(validationRoutine);
            validationRoutine = null;
        }

        isBusy = false;
        currentInput = "";
        SetFeedbackActive(failFeedbackObject, false);
        SetFeedbackActive(successFeedbackObject, false);
        ClearDisplaySlots();
    }

    private IEnumerator ValidateInput()
    {
        isBusy = true;

        if (lastKeyRevealDelay > 0f)
            yield return new WaitForSeconds(lastKeyRevealDelay);

        if (currentInput == NormalizeCode(correctCode))
            yield return PlaySuccess();
        else
            yield return PlayFailure();

        validationRoutine = null;
    }

    private IEnumerator PlaySuccess()
    {
        isUnlocked = true;
        SetFeedbackActive(failFeedbackObject, false);
        SetFeedbackActive(successFeedbackObject, true);

        if (successFeedbackDuration > 0f)
            yield return new WaitForSeconds(successFeedbackDuration);

        SetFeedbackActive(successFeedbackObject, false);
        UnlockSafe();
    }

    private IEnumerator PlayFailure()
    {
        SetFeedbackActive(successFeedbackObject, false);
        SetFeedbackActive(failFeedbackObject, true);

        if (failFeedbackDuration > 0f)
            yield return new WaitForSeconds(failFeedbackDuration);

        SetFeedbackActive(failFeedbackObject, false);
        currentInput = "";
        ClearDisplaySlots();
        isBusy = false;
        onFailed?.Invoke();
    }

    private void UnlockSafe()
    {
        if (safeOpenObject != null)
            safeOpenObject.SetActive(true);

        onUnlocked?.Invoke();

        if (safeClosedObject != null)
            safeClosedObject.SetActive(false);
    }

    private void ClearInput()
    {
        currentInput = "";
        ClearDisplaySlots();
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < CodeLength; i++)
        {
            Sprite sprite = i < currentInput.Length
                ? GetSpriteForKey(currentInput[i].ToString())
                : null;

            SetSlotSprite(i, sprite);
        }
    }

    private void ClearDisplaySlots()
    {
        for (int i = 0; i < CodeLength; i++)
            SetSlotSprite(i, null);
    }

    private void SetSlotSprite(int index, Sprite sprite)
    {
        if (index < 0 || index >= slotTargets.Length)
            return;

        SlotTarget target = slotTargets[index];
        if (!target.IsValid)
            return;

        target.SetSprite(sprite);
    }

    private Sprite GetSpriteForKey(string key)
    {
        key = NormalizeKey(key);

        if (key == "*")
            return starSprite;

        if (key == "#")
            return hashSprite;

        if (int.TryParse(key, out int digit)
            && digitSprites != null
            && digit >= 0
            && digit < digitSprites.Length)
        {
            return digitSprites[digit];
        }

        return null;
    }

    private void CacheSlotTargets()
    {
        for (int i = 0; i < CodeLength; i++)
        {
            GameObject slot = displaySlots != null && i < displaySlots.Length
                ? displaySlots[i]
                : null;

            slotTargets[i] = SlotTarget.From(slot);
        }
    }

    private static void SetFeedbackActive(GameObject feedbackObject, bool active)
    {
        if (feedbackObject != null)
            feedbackObject.SetActive(active);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "";

        string normalized = "";
        foreach (char character in code)
        {
            string key = NormalizeKey(character.ToString());
            if (!string.IsNullOrEmpty(key))
                normalized += key;

            if (normalized.Length >= CodeLength)
                break;
        }

        return normalized;
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        key = key.Trim();

        if (key == "star" || key == "Star")
            return "*";

        if (key == "hash" || key == "Hash")
            return "#";

        return key.Substring(0, 1);
    }

    private struct SlotTarget
    {
        private readonly Image image;
        private readonly SpriteRenderer spriteRenderer;

        public bool IsValid => image != null || spriteRenderer != null;

        private SlotTarget(Image image, SpriteRenderer spriteRenderer)
        {
            this.image = image;
            this.spriteRenderer = spriteRenderer;
        }

        public static SlotTarget From(GameObject slotObject)
        {
            if (slotObject == null)
                return new SlotTarget(null, null);

            Image image = slotObject.GetComponent<Image>();
            if (image == null)
                image = slotObject.GetComponentInChildren<Image>(true);

            SpriteRenderer spriteRenderer = slotObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = slotObject.GetComponentInChildren<SpriteRenderer>(true);

            return new SlotTarget(image, spriteRenderer);
        }

        public void SetSprite(Sprite sprite)
        {
            bool hasSprite = sprite != null;

            if (image != null)
            {
                image.sprite = sprite;
                image.enabled = hasSprite;
                image.preserveAspect = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.enabled = hasSprite;
            }
        }
    }
}
