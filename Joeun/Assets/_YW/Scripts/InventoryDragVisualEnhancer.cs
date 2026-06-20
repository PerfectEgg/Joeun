using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Adds drag-only shadow feedback to Wan inventory slots without changing their drag logic.
/// </summary>
[RequireComponent(typeof(Image))]
public sealed class InventoryDragVisualEnhancer : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] bool showShadow = true;
    [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] Vector2 shadowOffset = new Vector2(8f, -8f);
    [SerializeField, Min(0.1f)] float shadowScale = 1f;

    RectTransform rectTransform;
    Image iconImage;
    bool dragging;

    Transform shadowParent;
    RectTransform shadowRect;
    Image shadowImage;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        iconImage = GetComponent<Image>();
    }

    void OnDisable()
    {
        dragging = false;
        DestroyShadow();
    }

    void LateUpdate()
    {
        if (!dragging)
            return;

        if (!showShadow)
        {
            HideShadow();
            return;
        }

        EnsureShadow();
        UpdateShadow();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasVisibleItem())
            return;

        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        HideShadow();
    }

    bool HasVisibleItem()
    {
        return iconImage != null && iconImage.enabled && iconImage.sprite != null;
    }

    void EnsureShadow()
    {
        if (!showShadow || iconImage == null)
            return;

        Transform targetParent = transform.parent != null ? transform.parent : transform.root;
        if (targetParent == null)
            return;

        if (shadowRect == null)
        {
            GameObject shadow = new GameObject($"{name}_DragShadow", typeof(RectTransform), typeof(Image));
            shadowRect = shadow.GetComponent<RectTransform>();
            shadowImage = shadow.GetComponent<Image>();
        }

        if (shadowParent != targetParent)
        {
            shadowParent = targetParent;
            shadowRect.SetParent(shadowParent, false);
        }

        shadowRect.anchorMin = rectTransform.anchorMin;
        shadowRect.anchorMax = rectTransform.anchorMax;
        shadowRect.pivot = rectTransform.pivot;
        shadowRect.sizeDelta = rectTransform.sizeDelta;

        shadowImage.sprite = iconImage.sprite;
        shadowImage.type = iconImage.type;
        shadowImage.preserveAspect = iconImage.preserveAspect;
        shadowImage.raycastTarget = false;
        shadowImage.color = shadowColor;
        shadowImage.enabled = true;
        shadowImage.gameObject.SetActive(true);
    }

    void UpdateShadow()
    {
        if (!showShadow || shadowRect == null || rectTransform == null)
            return;

        shadowRect.position = rectTransform.position + (Vector3)shadowOffset;
        shadowRect.rotation = rectTransform.rotation;
        shadowRect.localScale = rectTransform.localScale * shadowScale;
        shadowRect.sizeDelta = rectTransform.sizeDelta;

        shadowRect.SetAsLastSibling();
        transform.SetAsLastSibling();
    }

    void HideShadow()
    {
        if (shadowImage == null)
            return;

        shadowImage.gameObject.SetActive(false);
    }

    void DestroyShadow()
    {
        if (shadowRect == null)
            return;

        Destroy(shadowRect.gameObject);
        shadowRect = null;
        shadowImage = null;
        shadowParent = null;
    }
}
