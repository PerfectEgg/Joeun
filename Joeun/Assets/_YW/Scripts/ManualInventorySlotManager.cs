using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ManualInventorySlotManager : MonoBehaviour
{
    [Serializable]
    private class FixedItemSlot
    {
        public ItemData itemData;
        public RectTransform slot;
    }

    [Header("Item UI")]
    [SerializeField] private GameObject uiItemPrefab;
    [SerializeField] private Vector2 slotPadding = new Vector2(10f, 10f);

    [Header("Manual Slots")]
    [SerializeField] private List<RectTransform> slots = new List<RectTransform>();
    [SerializeField] private List<FixedItemSlot> fixedItemSlots = new List<FixedItemSlot>();

    private readonly List<ItemData> inventoryList = new List<ItemData>();
    private readonly Dictionary<ItemData, GameObject> itemViews = new Dictionary<ItemData, GameObject>();
    private static readonly FieldInfo IconImageField = typeof(UIInventoryItem).GetField("_iconImage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo OriginPosField = typeof(UIInventoryItem).GetField("_originPos", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo OriginScaleField = typeof(UIInventoryItem).GetField("_originScale", BindingFlags.Instance | BindingFlags.NonPublic);

    private void OnEnable()
    {
        GameEvent.EOnItemCollected += AddItem;
        GameEvent.EOnItemUsed += RemoveItem;
    }

    private void OnDisable()
    {
        GameEvent.EOnItemCollected -= AddItem;
        GameEvent.EOnItemUsed -= RemoveItem;
    }

    private void AddItem(ItemData itemData)
    {
        if (itemData == null || uiItemPrefab == null)
            return;

        if (itemViews.ContainsKey(itemData))
            return;

        RectTransform targetSlot = FindFixedSlot(itemData);
        if (targetSlot == null)
            targetSlot = FindFirstEmptySlot();

        if (targetSlot == null)
        {
            DevLog.LogWarning($"[ManualInventorySlotManager] Empty inventory slot not found: {itemData.itemName}");
            return;
        }

        GameObject itemObject = Instantiate(uiItemPrefab, targetSlot);
        FitItemToSlot(itemObject, targetSlot);

        SetupItemObject(itemObject, itemData);

        inventoryList.Add(itemData);
        itemViews[itemData] = itemObject;
    }

    private void RemoveItem(ItemData itemData)
    {
        if (itemData == null)
            return;

        inventoryList.Remove(itemData);

        if (!itemViews.TryGetValue(itemData, out GameObject itemObject))
            return;

        itemViews.Remove(itemData);

        if (itemObject != null)
            Destroy(itemObject);
    }

    private RectTransform FindFixedSlot(ItemData itemData)
    {
        foreach (FixedItemSlot fixedSlot in fixedItemSlots)
        {
            if (fixedSlot == null || fixedSlot.itemData != itemData)
                continue;

            if (fixedSlot.slot != null)
                return fixedSlot.slot;
        }

        return null;
    }

    private RectTransform FindFirstEmptySlot()
    {
        foreach (RectTransform slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.childCount == 0)
                return slot;
        }

        return null;
    }

    private void FitItemToSlot(GameObject itemObject, RectTransform slot)
    {
        if (itemObject == null || slot == null)
            return;

        RectTransform rect = itemObject.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.SetParent(slot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = slotPadding;
        rect.offsetMax = -slotPadding;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;

        LayoutElement layoutElement = itemObject.GetComponent<LayoutElement>();
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
    }

    private void SetupItemObject(GameObject itemObject, ItemData itemData)
    {
        Image image = itemObject.GetComponent<Image>();
        if (image == null)
            image = itemObject.GetComponentInChildren<Image>(true);

        if (image != null)
        {
            image.sprite = itemData.itemIcon;
            image.preserveAspect = true;
        }

        UIInventoryItem uiItem = itemObject.GetComponent<UIInventoryItem>();
        if (uiItem == null)
            uiItem = itemObject.GetComponentInChildren<UIInventoryItem>(true);

        if (uiItem == null)
            return;

        uiItem._myItemData = itemData;

        if (image != null)
            IconImageField?.SetValue(uiItem, image);

        Transform itemTransform = uiItem.transform;
        OriginPosField?.SetValue(uiItem, itemTransform.position);
        OriginScaleField?.SetValue(uiItem, itemTransform.localScale);
    }
}
