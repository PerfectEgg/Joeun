using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecognitionDecodeInventoryController : MonoBehaviour
{
    static readonly Color LockedIconColor = new Color(0.22f, 0.17f, 0.24f, 0.62f);
    static readonly Color UnlockedIconColor = Color.white;

    [SerializeField] GameObject inventoryRoot;
    [SerializeField] UIDecode[] slots = new UIDecode[12];
    [SerializeField] DecodeData[] decodeData;
    [SerializeField] LetterBankController letterBank;
    [SerializeField] bool sortAlphabetically = true;
    [SerializeField] bool refreshOnEnable = true;
    [SerializeField] bool showOnEnable = true;
    [SerializeField] bool showLockedLetters = true;

    readonly Dictionary<char, DecodeData> runtimeDataByLetter = new Dictionary<char, DecodeData>();

    void Awake()
    {
        if (inventoryRoot == null)
            inventoryRoot = gameObject;

        CollectSlotsIfNeeded();
        AutoWireBank();
    }

    void OnEnable()
    {
        AutoWireBank();
        SubscribeBank();

        if (showOnEnable)
            Open();

        if (refreshOnEnable)
            Refresh();
    }

    void OnDisable()
    {
        UnsubscribeBank();
    }

    public void Open()
    {
        if (inventoryRoot != null)
            inventoryRoot.SetActive(true);
    }

    public void Close()
    {
        if (inventoryRoot != null)
            inventoryRoot.SetActive(false);
    }

    public void Refresh()
    {
        CollectSlotsIfNeeded();

        List<DecodeData> data = BuildDataList();

        if (sortAlphabetically)
            data.Sort((a, b) => a.decodeLetter.CompareTo(b.decodeLetter));

        for (int i = 0; i < slots.Length; i++)
        {
            UIDecode slot = slots[i];
            if (slot == null)
                continue;

            if (i < data.Count)
                SetupSlot(slot, data[i]);
            else
                slot.Clear();
        }
    }

    public void SetDecodeData(DecodeData[] data)
    {
        decodeData = data;
        Refresh();
    }

    void SetupSlot(UIDecode slot, DecodeData data)
    {
        slot.Setup(data);

        Image image = slot.GetComponent<Image>();
        bool unlocked = IsUnlocked(data);

        if (!showLockedLetters && !unlocked)
        {
            slot.Clear();
            return;
        }

        if (image != null)
        {
            image.enabled = true;
            image.raycastTarget = unlocked;
            image.color = unlocked ? UnlockedIconColor : LockedIconColor;
        }

        CanvasGroup canvasGroup = slot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = unlocked;
            canvasGroup.blocksRaycasts = unlocked;
        }
    }

    bool IsUnlocked(DecodeData data)
    {
        if (data == null)
            return false;

        if (letterBank == null)
            return true;

        return letterBank.IsUnlocked(data.decodeLetter);
    }

    List<DecodeData> BuildDataList()
    {
        Dictionary<char, DecodeData> byLetter = new Dictionary<char, DecodeData>();

        if (decodeData != null)
        {
            foreach (DecodeData data in decodeData)
                AddData(byLetter, data);
        }

        if (showLockedLetters)
        {
            AddLetterBankData(byLetter);
            AddResourceLetterData(byLetter);
        }

        return new List<DecodeData>(byLetter.Values);
    }

    void AddLetterBankData(Dictionary<char, DecodeData> byLetter)
    {
        AutoWireBank();

        if (letterBank == null)
            return;

        LetterBankSlot[] bankSlots = letterBank.GetComponentsInChildren<LetterBankSlot>(true);
        foreach (LetterBankSlot bankSlot in bankSlots)
        {
            if (bankSlot == null)
                continue;

            AddData(byLetter, GetOrCreateRuntimeData(bankSlot.Letter));
        }

        LetterBankLetterView[] bankViews = letterBank.GetComponentsInChildren<LetterBankLetterView>(true);
        foreach (LetterBankLetterView bankView in bankViews)
        {
            if (bankView == null)
                continue;

            AddData(byLetter, GetOrCreateRuntimeData(bankView.Letter));
        }
    }

    void AddResourceLetterData(Dictionary<char, DecodeData> byLetter)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("DecodeLetters");
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
                continue;

            string spriteName = sprite.name;
            if (!spriteName.StartsWith("letter_", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = spriteName.Substring("letter_".Length);
            if (suffix.Length != 1 || !char.IsLetter(suffix[0]))
                continue;

            AddData(byLetter, GetOrCreateRuntimeData(char.ToUpperInvariant(suffix[0]), sprite));
        }
    }

    void AddData(Dictionary<char, DecodeData> byLetter, DecodeData data)
    {
        if (data == null || !char.IsLetter(data.decodeLetter))
            return;

        char letter = char.ToUpperInvariant(data.decodeLetter);
        if (byLetter.ContainsKey(letter))
            return;

        data.decodeLetter = letter;
        byLetter.Add(letter, data);
    }

    DecodeData GetOrCreateRuntimeData(char letter, Sprite sprite = null)
    {
        if (!char.IsLetter(letter))
            return null;

        char normalized = char.ToUpperInvariant(letter);
        if (runtimeDataByLetter.TryGetValue(normalized, out DecodeData data) && data != null)
            return data;

        data = ScriptableObject.CreateInstance<DecodeData>();
        data.name = $"Runtime Decode {normalized}";
        data.decodeLetter = normalized;
        data.decodeIcon = sprite != null ? sprite : LoadLetterSprite(normalized);
        runtimeDataByLetter[normalized] = data;
        return data;
    }

    static Sprite LoadLetterSprite(char letter)
    {
        string path = $"DecodeLetters/letter_{char.ToUpperInvariant(letter)}";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    void CollectSlotsIfNeeded()
    {
        bool hasSlot = false;
        foreach (UIDecode slot in slots)
        {
            if (slot != null)
            {
                hasSlot = true;
                break;
            }
        }

        if (hasSlot)
            return;

        UIDecode[] found = GetComponentsInChildren<UIDecode>(true);
        for (int i = 0; i < slots.Length && i < found.Length; i++)
            slots[i] = found[i];
    }

    void AutoWireBank()
    {
        if (letterBank != null)
            return;

        letterBank = GetComponentInParent<LetterBankController>();

        if (letterBank == null)
            letterBank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);
    }

    void SubscribeBank()
    {
        if (letterBank == null)
            return;

        letterBank.Changed -= Refresh;
        letterBank.Changed += Refresh;
    }

    void UnsubscribeBank()
    {
        if (letterBank == null)
            return;

        letterBank.Changed -= Refresh;
    }
}
