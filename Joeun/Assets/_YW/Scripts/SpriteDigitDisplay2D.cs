using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpriteDigitDisplay2D : MonoBehaviour
{
    [SerializeField] private TransformerDialControl2D sourceDial;
    [SerializeField] private bool autoSubscribeToSource = true;

    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private Sprite blankSprite;
    [SerializeField] private List<SpriteRenderer> digitSlots = new List<SpriteRenderer>();
    [SerializeField] private bool padWithZeros = true;

    private void Reset()
    {
        sourceDial = GetComponentInParent<TransformerDialControl2D>();
    }

    private void Awake()
    {
        ResolveSourceDial();
        ShowCurrentValue();
    }

    private void OnEnable()
    {
        ResolveSourceDial();

        if (sourceDial != null && autoSubscribeToSource)
            sourceDial.ValueChanged += HandleDialValueChanged;

        ShowCurrentValue();
    }

    private void OnDisable()
    {
        if (sourceDial != null)
            sourceDial.ValueChanged -= HandleDialValueChanged;
    }

    private void ResolveSourceDial()
    {
        if (sourceDial == null)
            sourceDial = GetComponentInParent<TransformerDialControl2D>();
    }

    public void ShowValue(float value)
    {
        int slotCount = digitSlots.Count;
        if (slotCount == 0)
            return;

        int intValue = Mathf.Max(0, Mathf.RoundToInt(value));
        string text = intValue.ToString();

        if (text.Length > slotCount)
            text = text.Substring(text.Length - slotCount);

        int pad = slotCount - text.Length;

        for (int i = 0; i < slotCount; i++)
        {
            SpriteRenderer slot = digitSlots[i];
            if (slot == null)
                continue;

            if (i < pad)
            {
                slot.sprite = padWithZeros ? GetDigit(0) : blankSprite;
                slot.enabled = slot.sprite != null;
                continue;
            }

            int digit = text[i - pad] - '0';
            slot.sprite = GetDigit(digit);
            slot.enabled = slot.sprite != null;
        }
    }

    private void ShowCurrentValue()
    {
        ShowValue(sourceDial != null ? sourceDial.Value : 0f);
    }

    private void HandleDialValueChanged(TransformerDialControl2D dial, float value)
    {
        ShowValue(value);
    }

    private Sprite GetDigit(int digit)
    {
        if (digit >= 0 && digit < digitSprites.Length)
            return digitSprites[digit];

        return null;
    }
}
