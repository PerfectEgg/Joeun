using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecognitionDecodeInventoryController : MonoBehaviour
{
    [SerializeField] GameObject inventoryRoot;
    [SerializeField] UIDecode[] slots = new UIDecode[12];
    [SerializeField] DecodeData[] decodeData;
    [SerializeField] bool sortAlphabetically = true;
    [SerializeField] bool refreshOnEnable = true;
    [SerializeField] bool showOnEnable = true;

    void Awake()
    {
        if (inventoryRoot == null)
            inventoryRoot = gameObject;

        CollectSlotsIfNeeded();
    }

    void OnEnable()
    {
        if (showOnEnable)
            Open();

        if (refreshOnEnable)
            Refresh();
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

        List<DecodeData> data = new List<DecodeData>();
        if (decodeData != null)
            data.AddRange(decodeData);

        data.RemoveAll(item => item == null);

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
        if (image == null)
            return;

        image.enabled = true;
        image.raycastTarget = true;
        Color color = image.color;
        color.a = 1f;
        image.color = color;
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
}
