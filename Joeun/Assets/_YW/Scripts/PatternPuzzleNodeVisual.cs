using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PatternPuzzleNodeVisual : MonoBehaviour
{
    private const string LitOverlayObjectName = "__PatternPuzzleLitOverlay";

    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite litSprite;
    [SerializeField] private UnityEngine.Object litOverlayAsset;
    [SerializeField] private Sprite startSprite;
    [SerializeField] private Sprite goalSprite;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField, Range(0f, 1f)] private float litOverlayAlpha = 0.9f;

    private bool isStart;
    private bool isGoal;
    private bool isLit;
    private bool hasColorOverride;
    private Color colorOverride = Color.white;
    private Image litOverlayImage;
    private Texture2D runtimeLitOverlayTexture;
    private Sprite runtimeLitOverlaySprite;

    public void Configure(
        GridSlot slot,
        Sprite idleOverride = null,
        Sprite litOverride = null,
        Sprite startOverride = null,
        Sprite goalOverride = null,
        UnityEngine.Object litOverlayAssetOverride = null)
    {
        EnsureTargetImage();
        EnsureSprites();

        if (idleOverride != null)
            idleSprite = idleOverride;

        if (litOverride != null)
            litSprite = litOverride;

        if (startOverride != null)
            startSprite = startOverride;

        if (goalOverride != null)
            goalSprite = goalOverride;

        if (litOverlayAssetOverride != null)
            SetLitOverlayAsset(litOverlayAssetOverride);

        isStart = slot != null && slot.isStart;
        isGoal = slot != null && slot.isGoal;
        Apply();
    }

    public void SetLitOverlayAsset(UnityEngine.Object asset)
    {
        if (litOverlayAsset == asset)
            return;

        litOverlayAsset = asset;
        ClearRuntimeLitOverlaySprite();
        Apply();
    }

    public void SetLit(bool lit)
    {
        isLit = lit;
        Apply();
    }

    public void SetColorOverride(Color color)
    {
        hasColorOverride = true;
        colorOverride = color;
        Apply();
    }

    public void ClearColorOverride()
    {
        hasColorOverride = false;
        Apply();
    }

    private void Reset()
    {
        EnsureTargetImage();
        EnsureSprites();
        Apply();
    }

    private void OnEnable()
    {
        EnsureTargetImage();
        EnsureSprites();
        Apply();
    }

    private void OnDestroy()
    {
        ClearRuntimeLitOverlaySprite();
    }

    private void EnsureTargetImage()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void EnsureSprites()
    {
        if (idleSprite == null)
            idleSprite = LoadSprite("PatternPuzzle/arrow0");

        if (litSprite == null)
            litSprite = LoadSprite("PatternPuzzle/startblock");

        if (litSprite == null)
            litSprite = LoadSprite("PatternPuzzle/decoding2");

        if (litSprite == null)
            litSprite = LoadSprite("PatternPuzzle/decoding");

        if (goalSprite == null)
            goalSprite = LoadSprite("PatternPuzzle/goalblock");
    }

    private void Apply()
    {
        if (targetImage == null)
            return;

        Sprite nextSprite = idleSprite;

        if (isStart && startSprite != null)
            nextSprite = startSprite;
        else if (isGoal && goalSprite != null)
            nextSprite = goalSprite;

        if (nextSprite != null)
            targetImage.sprite = nextSprite;

        targetImage.color = hasColorOverride ? colorOverride : Color.white;
        targetImage.preserveAspect = preserveAspect;

        ApplyLitOverlay();
    }

    private void ApplyLitOverlay()
    {
        Sprite overlaySprite = ResolveLitOverlaySprite();
        if (overlaySprite == null)
        {
            if (litOverlayImage != null)
                litOverlayImage.enabled = false;

            return;
        }

        EnsureLitOverlayImage();

        if (litOverlayImage == null)
            return;

        litOverlayImage.sprite = overlaySprite;
        litOverlayImage.preserveAspect = preserveAspect;
        litOverlayImage.raycastTarget = false;
        litOverlayImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(litOverlayAlpha));
        litOverlayImage.enabled = isLit;
        litOverlayImage.transform.SetAsLastSibling();
    }

    private void EnsureLitOverlayImage()
    {
        if (litOverlayImage != null)
            return;

        Transform existing = transform.Find(LitOverlayObjectName);
        GameObject overlayObject = existing != null
            ? existing.gameObject
            : new GameObject(LitOverlayObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            overlayObject.transform.SetParent(transform, false);

        RectTransform rect = overlayObject.transform as RectTransform;
        Stretch(rect);

        litOverlayImage = overlayObject.GetComponent<Image>();
    }

    private Sprite ResolveLitOverlaySprite()
    {
        if (litOverlayAsset is Sprite assetSprite)
            return assetSprite;

        if (litOverlayAsset is Texture2D assetTexture)
            return GetOrCreateRuntimeLitOverlaySprite(assetTexture);

        return litSprite;
    }

    private Sprite GetOrCreateRuntimeLitOverlaySprite(Texture2D texture)
    {
        if (texture == null)
            return null;

        if (runtimeLitOverlaySprite != null && runtimeLitOverlayTexture == texture)
            return runtimeLitOverlaySprite;

        ClearRuntimeLitOverlaySprite();
        runtimeLitOverlayTexture = texture;
        runtimeLitOverlaySprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return runtimeLitOverlaySprite;
    }

    private void ClearRuntimeLitOverlaySprite()
    {
        if (runtimeLitOverlaySprite != null)
        {
            Destroy(runtimeLitOverlaySprite);
            runtimeLitOverlaySprite = null;
        }

        runtimeLitOverlayTexture = null;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }
}
