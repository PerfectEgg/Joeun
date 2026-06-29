using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LetterBankController : MonoBehaviour
{
    [SerializeField] private LetterBankSlot[] slots = Array.Empty<LetterBankSlot>();
    [SerializeField] private string initiallyUnlockedLetters = string.Empty;
    [SerializeField] private bool resetOnAwake = true;
    [SerializeField] private bool autoCollectChildSlots = true;

    private readonly HashSet<char> unlockedLetters = new HashSet<char>();

    public event Action<char> LetterUnlocked;
    public event Action Changed;

    private void Awake()
    {
        CollectSlotsIfNeeded();

        foreach (LetterBankSlot slot in slots)
        {
            if (slot != null)
                slot.SetBank(this);
        }

        if (resetOnAwake)
            ResetBank();
        else
            ApplySlots(true);
    }

    private void OnEnable()
    {
        ApplySlots(true);
    }

    public bool IsUnlocked(char letter)
    {
        return unlockedLetters.Contains(Normalize(letter));
    }

    public string GetUnlockedLetters()
    {
        List<char> letters = new List<char>(unlockedLetters);
        letters.Sort();
        return new string(letters.ToArray());
    }

    public void ResetBank()
    {
        unlockedLetters.Clear();
        UnlockLettersInternal(initiallyUnlockedLetters, true);
        ApplySlots(true);
        Changed?.Invoke();
    }

    public void UnlockLetter(char letter)
    {
        UnlockLetterInternal(letter, false);
    }

    public void UnlockLetterString(string letter)
    {
        if (string.IsNullOrEmpty(letter))
            return;

        UnlockLetterInternal(letter[0], false);
    }

    public void UnlockLetters(string letters)
    {
        UnlockLettersInternal(letters, false);
    }

    public void UnlockLettersInstant(string letters)
    {
        UnlockLettersInternal(letters, true);
    }

    public void LockLetter(char letter)
    {
        char normalized = Normalize(letter);
        if (normalized == '\0' || !unlockedLetters.Remove(normalized))
            return;

        ApplySlot(normalized, false, true);
        Changed?.Invoke();
    }

    void UnlockLettersInternal(string letters, bool instant)
    {
        if (string.IsNullOrEmpty(letters))
            return;

        bool changed = false;
        foreach (char letter in letters)
            changed |= UnlockLetterInternal(letter, instant, false);

        if (changed)
            Changed?.Invoke();
    }

    bool UnlockLetterInternal(char letter, bool instant, bool notifyChanged = true)
    {
        char normalized = Normalize(letter);
        if (normalized == '\0' || unlockedLetters.Contains(normalized))
            return false;

        unlockedLetters.Add(normalized);
        ApplySlot(normalized, true, instant);

        if (!instant)
            LetterUnlocked?.Invoke(normalized);

        if (notifyChanged)
            Changed?.Invoke();

        return true;
    }

    void ApplySlots(bool instant)
    {
        CollectSlotsIfNeeded();

        foreach (LetterBankSlot slot in slots)
        {
            if (slot == null)
                continue;

            slot.SetBank(this);
            slot.SetUnlocked(IsUnlocked(slot.Letter), instant);
        }
    }

    void ApplySlot(char letter, bool unlocked, bool instant)
    {
        CollectSlotsIfNeeded();

        foreach (LetterBankSlot slot in slots)
        {
            if (slot != null && Normalize(slot.Letter) == letter)
                slot.SetUnlocked(unlocked, instant);
        }
    }

    void CollectSlotsIfNeeded()
    {
        if (!autoCollectChildSlots)
            return;

        bool hasSlot = false;
        foreach (LetterBankSlot slot in slots)
        {
            if (slot != null)
            {
                hasSlot = true;
                break;
            }
        }

        if (hasSlot)
            return;

        slots = GetComponentsInChildren<LetterBankSlot>(true);
    }

    static char Normalize(char letter)
    {
        if (!char.IsLetter(letter))
            return '\0';

        return char.ToUpperInvariant(letter);
    }
}
