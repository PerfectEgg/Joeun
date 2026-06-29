using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds an assemble outline from SpriteRenderer alpha, matching the arm assemble outline behavior
/// for world-space sprites.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpriteRendererAssembleOutlineTarget : MonoBehaviour
{
    const string OutlineObjectName = "__SpriteAssembleOutline";
    const string HoverObjectName = "__SpriteAssembleOutlineHover";

    const float IdleAlpha = 0.48f;
    const float PulseAlpha = 0.06f;
    const float PulseSpeed = 3.4f;
    const float FadeDuration = 0.12f;
    const float HoverFadeDuration = 0.08f;
    const float AlphaThreshold = 0.1f;
    const float BaseGlowOpacity = 0.34f;
    const float HoverGlowOpacity = 0.52f;

    const int PaddingPixels = 16;
    const int BaseOutlinePixels = 2;
    const int BaseGlowPixels = 8;
    const int HoverOutlinePixels = 3;
    const int HoverGlowPixels = 12;
    const int SortingOrderOffset = 140;

    [SerializeField] HandPuzzleController handPuzzle;
    [SerializeField] SpriteRenderer sourceRenderer;
    [SerializeField] bool forceVisibleForPreview;

    readonly HashSet<Texture2D> unreadableTextures = new HashSet<Texture2D>();

    SpriteRenderer outlineRenderer;
    SpriteRenderer hoverRenderer;
    Texture2D outlineTexture;
    Texture2D hoverTexture;
    Sprite outlineSprite;
    Sprite hoverSprite;
    Sprite sourceSpriteSnapshot;
    Color sourceColorSnapshot;
    bool textureDirty = true;
    bool canUseAlphaMask;
    float currentAlpha;
    float hoverBlend;

    Color assembleColor = new Color(0f, 1f, 1f, 1f);

    public void Bind(HandPuzzleController controller, SpriteRenderer source)
    {
        handPuzzle = controller;

        if (source == null || sourceRenderer == source)
            return;

        sourceRenderer = source;
        textureDirty = true;
        EnsureRenderers();
    }

    public void PrepareOutline()
    {
        ResolveController();
        ResolveSource();
        EnsureRenderers();

        if (textureDirty || NeedsRebuild())
            RebuildOutline();

        currentAlpha = 0f;
        hoverBlend = 0f;
        ApplyAlpha();
    }

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        ResolveSource();
        if (!IsRenderable(sourceRenderer))
            return false;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Vector3 screen = new Vector3(
            screenPoint.x,
            screenPoint.y,
            Mathf.Abs(camera.transform.position.z - sourceRenderer.transform.position.z));
        Vector3 worldPoint = camera.ScreenToWorldPoint(screen);

        if (!sourceRenderer.bounds.Contains(worldPoint))
            return false;

        float alpha = SampleAlpha(worldPoint, false);
        return canUseAlphaMask ? alpha >= AlphaThreshold : true;
    }

    void Awake()
    {
        ResolveController();
        ResolveSource();
        EnsureRenderers();
    }

    void OnEnable()
    {
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;
    }

    void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;
        currentAlpha = 0f;
        hoverBlend = 0f;
        ApplyAlpha();
    }

    void OnDestroy()
    {
        ClearGeneratedAssets();
    }

    void OnValidate()
    {
        textureDirty = true;
    }

    void LateUpdate()
    {
        ResolveController();
        ResolveSource();
        EnsureRenderers();

        bool shouldShow = ShouldShow();
        if (shouldShow && (textureDirty || NeedsRebuild()))
            RebuildOutline();

        if (shouldShow)
            SyncRenderers();

        float targetAlpha = shouldShow && outlineSprite != null ? AnimatedAlpha() : 0f;
        float step = FadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / FadeDuration;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, step);

        bool hover = shouldShow && ContainsScreenPoint(Input.mousePosition);
        float hoverTarget = hover ? 1f : 0f;
        float hoverStep = HoverFadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / HoverFadeDuration;
        hoverBlend = Mathf.MoveTowards(hoverBlend, hoverTarget, hoverStep);

        ApplyAlpha();
    }

    void HandleSkillModeChanged(SkillModeType mode)
    {
        ApplyAlpha();
    }

    void HandleInteractionLockChanged(bool locked)
    {
        ApplyAlpha();
    }

    bool ShouldShow()
    {
        if (SkillInteractionLock.IsLocked)
            return false;

        if (forceVisibleForPreview)
            return true;

        if (SkillIconModeView.CurrentMode != SkillModeType.Assemble)
            return false;

        ResolveController();
        return handPuzzle != null && handPuzzle.IsReadyForAssemble;
    }

    float AnimatedAlpha()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) * 0.5f;
        return Mathf.Clamp01(IdleAlpha + pulse * PulseAlpha);
    }

    void ResolveController()
    {
        if (handPuzzle != null)
            return;

        handPuzzle = GetComponent<HandPuzzleController>();
        if (handPuzzle == null)
            handPuzzle = GetComponentInParent<HandPuzzleController>();
        if (handPuzzle == null)
            handPuzzle = GetComponentInChildren<HandPuzzleController>(true);
    }

    void ResolveSource()
    {
        if (IsRenderable(sourceRenderer))
            return;

        Transform hand = transform.Find("Hand");
        if (hand != null && IsUsableSource(hand.GetComponent<SpriteRenderer>()))
        {
            sourceRenderer = hand.GetComponent<SpriteRenderer>();
            textureDirty = true;
            return;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (!IsUsableSource(renderer))
                continue;

            if (renderer.GetComponentInParent<HandPuzzlePiece>() != null)
                continue;

            if (renderer.GetComponentInParent<HandPuzzleSlot>() != null)
                continue;

            if (renderer.GetComponent<ToolItem>() != null)
                continue;

            sourceRenderer = renderer;
            textureDirty = true;
            return;
        }
    }

    static bool IsUsableSource(SpriteRenderer renderer)
    {
        return renderer != null
            && renderer.sprite != null
            && !renderer.name.StartsWith("__", System.StringComparison.Ordinal);
    }

    static bool IsRenderable(SpriteRenderer renderer)
    {
        return renderer != null
            && renderer.enabled
            && renderer.gameObject.activeInHierarchy
            && renderer.sprite != null;
    }

    void EnsureRenderers()
    {
        if (!IsUsableSource(sourceRenderer))
            return;

        if (outlineRenderer == null)
            outlineRenderer = EnsureChildRenderer(OutlineObjectName);
        if (hoverRenderer == null)
            hoverRenderer = EnsureChildRenderer(HoverObjectName);

        outlineRenderer.enabled = false;
        hoverRenderer.enabled = false;
        SyncRenderers();
    }

    SpriteRenderer EnsureChildRenderer(string objectName)
    {
        Transform existing = sourceRenderer.transform.Find(objectName);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(objectName);
        if (existing == null)
            obj.transform.SetParent(sourceRenderer.transform, false);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = obj.AddComponent<SpriteRenderer>();

        return renderer;
    }

    bool NeedsRebuild()
    {
        return outlineSprite == null
            || hoverSprite == null
            || sourceRenderer == null
            || sourceSpriteSnapshot != sourceRenderer.sprite
            || sourceColorSnapshot != sourceRenderer.color;
    }

    void RebuildOutline()
    {
        textureDirty = false;
        canUseAlphaMask = false;
        ClearGeneratedAssets();

        if (!IsUsableSource(sourceRenderer))
            return;

        Sprite sourceSprite = sourceRenderer.sprite;
        Texture2D texture = sourceSprite.texture;
        if (texture == null || !texture.isReadable)
        {
            WarnUnreadable(texture);
            return;
        }

        Rect textureRect;
        try
        {
            textureRect = sourceSprite.textureRect;
        }
        catch
        {
            textureRect = sourceSprite.rect;
        }

        int sourceX = Mathf.RoundToInt(textureRect.x);
        int sourceY = Mathf.RoundToInt(textureRect.y);
        int sourceWidth = Mathf.RoundToInt(textureRect.width);
        int sourceHeight = Mathf.RoundToInt(textureRect.height);
        if (sourceWidth <= 0 || sourceHeight <= 0)
            return;

        int width = sourceWidth + PaddingPixels * 2;
        int height = sourceHeight + PaddingPixels * 2;
        bool[] mask = new bool[width * height];

        for (int y = 0; y < sourceHeight; y++)
        {
            int row = (y + PaddingPixels) * width;
            for (int x = 0; x < sourceWidth; x++)
            {
                Color pixel = texture.GetPixel(sourceX + x, sourceY + y);
                if (pixel.a * sourceRenderer.color.a >= AlphaThreshold)
                    mask[row + x + PaddingPixels] = true;
            }
        }

        Color32[] outlinePixels = BuildOutlinePixels(mask, width, height, BaseOutlinePixels, BaseGlowPixels, BaseGlowOpacity);
        Color32[] hoverPixels = BuildOutlinePixels(mask, width, height, HoverOutlinePixels, HoverGlowPixels, HoverGlowOpacity);

        outlineTexture = CreateTexture(width, height, $"{sourceSprite.name}_AssembleOutline");
        outlineTexture.SetPixels32(outlinePixels);
        outlineTexture.Apply(false, false);

        hoverTexture = CreateTexture(width, height, $"{sourceSprite.name}_AssembleOutlineHover");
        hoverTexture.SetPixels32(hoverPixels);
        hoverTexture.Apply(false, false);

        Vector2 paddedPivot = sourceSprite.pivot + Vector2.one * PaddingPixels;
        Vector2 normalizedPivot = new Vector2(paddedPivot.x / width, paddedPivot.y / height);
        Rect spriteRect = new Rect(0f, 0f, width, height);

        outlineSprite = Sprite.Create(outlineTexture, spriteRect, normalizedPivot, sourceSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        hoverSprite = Sprite.Create(hoverTexture, spriteRect, normalizedPivot, sourceSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);

        outlineRenderer.sprite = outlineSprite;
        hoverRenderer.sprite = hoverSprite;
        sourceSpriteSnapshot = sourceSprite;
        sourceColorSnapshot = sourceRenderer.color;
        canUseAlphaMask = true;
        SyncRenderers();
    }

    void SyncRenderers()
    {
        if (sourceRenderer == null)
            return;

        SyncRenderer(outlineRenderer);
        SyncRenderer(hoverRenderer);
    }

    void SyncRenderer(SpriteRenderer target)
    {
        if (target == null)
            return;

        target.sortingLayerID = sourceRenderer.sortingLayerID;
        target.sortingOrder = sourceRenderer.sortingOrder + SortingOrderOffset;
        target.flipX = sourceRenderer.flipX;
        target.flipY = sourceRenderer.flipY;
    }

    void ApplyAlpha()
    {
        bool visible = currentAlpha > 0.001f && IsRenderable(sourceRenderer) && outlineSprite != null;
        ApplyRendererAlpha(outlineRenderer, visible, currentAlpha);
        ApplyRendererAlpha(hoverRenderer, visible && hoverBlend > 0.001f, currentAlpha * hoverBlend);
    }

    void ApplyRendererAlpha(SpriteRenderer renderer, bool visible, float alpha)
    {
        if (renderer == null)
            return;

        renderer.enabled = visible;
        Color color = assembleColor;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    float SampleAlpha(Vector3 worldPoint, bool warn)
    {
        if (!IsRenderable(sourceRenderer))
            return 0f;

        Sprite sprite = sourceRenderer.sprite;
        Texture2D texture = sprite.texture;
        if (texture == null || !texture.isReadable)
        {
            if (warn)
                WarnUnreadable(texture);
            return 0f;
        }

        Vector3 localPoint = sourceRenderer.transform.InverseTransformPoint(worldPoint);
        if (sourceRenderer.flipX)
            localPoint.x = -localPoint.x;
        if (sourceRenderer.flipY)
            localPoint.y = -localPoint.y;

        Bounds bounds = sprite.bounds;
        if (!bounds.Contains(localPoint))
            return 0f;

        float u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPoint.x);
        float v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPoint.y);

        Rect textureRect;
        try
        {
            textureRect = sprite.textureRect;
        }
        catch
        {
            textureRect = sprite.rect;
        }

        float textureU = (textureRect.x + textureRect.width * u) / texture.width;
        float textureV = (textureRect.y + textureRect.height * v) / texture.height;
        return sourceRenderer.color.a * texture.GetPixelBilinear(textureU, textureV).a;
    }

    void WarnUnreadable(Texture2D texture)
    {
        if (texture == null || !unreadableTextures.Add(texture))
            return;

        Debug.LogWarning(
            $"{nameof(SpriteRendererAssembleOutlineTarget)}: '{texture.name}' texture is not readable. Enable Read/Write to build the alpha-based assemble outline.",
            this);
    }

    static Texture2D CreateTexture(int width, int height, string textureName)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    void ClearGeneratedAssets()
    {
        DestroyGenerated(outlineSprite);
        DestroyGenerated(hoverSprite);
        DestroyGenerated(outlineTexture);
        DestroyGenerated(hoverTexture);

        outlineSprite = null;
        hoverSprite = null;
        outlineTexture = null;
        hoverTexture = null;

        if (outlineRenderer != null)
            outlineRenderer.sprite = null;
        if (hoverRenderer != null)
            hoverRenderer.sprite = null;
    }

    static void DestroyGenerated(Object obj)
    {
        if (obj == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(obj);
        else
#endif
            Destroy(obj);
    }

    static Color32[] BuildOutlinePixels(
        bool[] mask,
        int width,
        int height,
        int outlinePixels,
        int glowPixels,
        float glowOpacity)
    {
        int length = width * height;
        Color32[] pixels = new Color32[length];
        int maxRadius = Mathf.Max(0, outlinePixels) + Mathf.Max(0, glowPixels);
        if (maxRadius <= 0)
            return pixels;

        bool[] grown = (bool[])mask.Clone();
        bool[] next = new bool[length];

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            DilateOnePixel(grown, next, width, height);
            byte alpha = AlphaForRadius(radius, outlinePixels, glowPixels, glowOpacity);

            for (int i = 0; i < length; i++)
            {
                if (next[i] && !grown[i])
                    pixels[i] = new Color32(255, 255, 255, alpha);
            }

            bool[] swap = grown;
            grown = next;
            next = swap;
        }

        return pixels;
    }

    static byte AlphaForRadius(int radius, int outlinePixels, int glowPixels, float glowOpacity)
    {
        if (radius <= outlinePixels)
            return 255;

        if (glowPixels <= 0)
            return 0;

        float t = 1f - (radius - outlinePixels) / (float)(glowPixels + 1);
        float alpha = Mathf.Clamp01(t * t * glowOpacity);
        return (byte)Mathf.RoundToInt(alpha * 255f);
    }

    static void DilateOnePixel(bool[] source, bool[] target, int width, int height)
    {
        for (int i = 0; i < target.Length; i++)
            target[i] = false;

        for (int y = 0; y < height; y++)
        {
            int yMin = Mathf.Max(0, y - 1);
            int yMax = Mathf.Min(height - 1, y + 1);

            for (int x = 0; x < width; x++)
            {
                bool filled = false;
                int xMin = Mathf.Max(0, x - 1);
                int xMax = Mathf.Min(width - 1, x + 1);

                for (int yy = yMin; yy <= yMax && !filled; yy++)
                {
                    int row = yy * width;
                    for (int xx = xMin; xx <= xMax; xx++)
                    {
                        if (source[row + xx])
                        {
                            filled = true;
                            break;
                        }
                    }
                }

                target[y * width + x] = filled;
            }
        }
    }
}
