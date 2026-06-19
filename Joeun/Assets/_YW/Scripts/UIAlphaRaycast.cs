using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a UI Image receive clicks only on pixels whose alpha is above a threshold.
/// Useful for irregular item silhouettes that should not use the full RectTransform.
/// </summary>
[RequireComponent(typeof(Image))]
public class UIAlphaRaycast : MonoBehaviour
{
    [SerializeField, Range(0.001f, 1f)]
    float alphaThreshold = 0.1f;

    bool forceRaycastTarget = true;

    Image targetImage;

    void Awake()
    {
        Apply();
    }

    void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Apply();
    }
#endif

    [ContextMenu("Apply Alpha Raycast")]
    public void Apply()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage == null)
            return;

        if (forceRaycastTarget)
            targetImage.raycastTarget = true;

        targetImage.alphaHitTestMinimumThreshold = alphaThreshold;

#if UNITY_EDITOR
        var sprite = targetImage.sprite;
        if (sprite != null && sprite.texture != null && !sprite.texture.isReadable)
        {
            Debug.LogWarning(
                $"{nameof(UIAlphaRaycast)} on {name} needs Read/Write Enabled on texture '{sprite.texture.name}' for alpha hit testing.",
                this);
        }
#endif
    }
}
