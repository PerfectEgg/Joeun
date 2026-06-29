using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds one combined silhouette outline from the assembled arm part images.
/// Attach this to the common puzzle root, not to each arm part.
/// </summary>
[DisallowMultipleComponent]
public class AssembleCompositeOutlineTarget : MonoBehaviour
{
    const string OutlineObjectName = "__AssembleCompositeOutline";
    const string HoverObjectName = "__AssembleCompositeOutlineHover";

    const bool AutoFindPuzzlePartImages = true;
    const bool IncludeInactivePuzzleParts = true;
    const bool AutoFindOnlyArmParts = true;
    const bool UseFullTextureWhenImageMatchesTexture = true;
    const bool SkipUnreadableTextures = true;

    const float TextureSizeMatchTolerance = 2f;
    const float IdleAlpha = 0.48f;
    const float PulseAlpha = 0.06f;
    const float PulseSpeed = 3.4f;
    const float FadeInDuration = 0.12f;
    const float FadeOutDuration = 0.12f;
    const float HoverFadeDuration = 0.08f;
    const float AlphaThreshold = 0.1f;
    const float TexturePixelsPerUnit = 1f;
    const float BoundsPadding = 8f;
    const float BaseGlowOpacity = 0.34f;
    const float HoverGlowOpacity = 0.52f;

    const int MaxTextureSize = 1024;
    const int MergeGapPixels = 3;
    const int BaseOutlinePixels = 2;
    const int BaseGlowPixels = 8;
    const int HoverOutlinePixels = 3;
    const int HoverGlowPixels = 12;
    const int SortingOrderOffset = 140;

    [Header("Sources")]
    [SerializeField] RectTransform outlineRoot;
    [SerializeField] Image[] sourceImages;

    [Header("Preview")]
    [SerializeField] bool forceVisibleForPreview;

    Color assembleColor = new Color(0f, 1f, 1f, 1f);

    readonly List<Image> resolvedSources = new List<Image>();
    readonly List<SourceSample> sourceSamples = new List<SourceSample>();
    readonly List<SourceSnapshot> snapshots = new List<SourceSnapshot>();
    readonly HashSet<Texture2D> unreadableTextures = new HashSet<Texture2D>();

    RectTransform rootRect;
    RectTransform outlineRect;
    RectTransform hoverRect;
    RawImage outlineImage;
    RawImage hoverImage;
    Canvas outlineCanvas;
    Texture2D outlineTexture;
    Texture2D hoverTexture;
    Rect outlineLocalBounds;
    ShowCompletedAssembly assemblyGate;
    bool textureDirty = true;
    float currentAlpha;
    float hoverBlend;
    bool isHovering;

    void Reset()
    {
        outlineRoot = transform as RectTransform;
    }

    void Awake()
    {
        ResolveRoot();
        EnsureOutlineObject();
    }

    void OnEnable()
    {
        ResolveRoot();
        EnsureOutlineObject();
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;
        ApplyMode(SkillIconModeView.CurrentMode, true);
    }

    void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;
        currentAlpha = 0f;
        hoverBlend = 0f;
        isHovering = false;
        ApplyAlpha();
    }

    void OnDestroy()
    {
        ReleaseTextures();
    }

    void OnValidate()
    {
        textureDirty = true;
    }

    void LateUpdate()
    {
        ResolveRoot();
        ResolveAssemblyGate();
        EnsureOutlineObject();

        bool shouldShow = ShouldShow(SkillIconModeView.CurrentMode);
        if (shouldShow && (textureDirty || NeedsRebuild()))
            RebuildOutline();

        if (shouldShow)
        {
            UpdateHoverState();
            SyncSortingCanvas();
        }
        else
        {
            isHovering = false;
        }

        float targetAlpha = shouldShow && outlineTexture != null ? AnimatedAlpha() : 0f;
        float fadeDuration = shouldShow ? FadeInDuration : FadeOutDuration;
        float alphaStep = fadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / fadeDuration;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, alphaStep);

        float hoverTarget = isHovering ? 1f : 0f;
        float hoverStep = HoverFadeDuration <= 0f ? 1f : Time.unscaledDeltaTime / HoverFadeDuration;
        hoverBlend = Mathf.MoveTowards(hoverBlend, hoverTarget, hoverStep);

        ApplyAlpha();
    }

    void HandleSkillModeChanged(SkillModeType mode)
    {
        ApplyMode(mode, false);
    }

    void HandleInteractionLockChanged(bool locked)
    {
        ApplyMode(SkillIconModeView.CurrentMode, false);
    }

    void ApplyMode(SkillModeType mode, bool immediate)
    {
        bool shouldShow = ShouldShow(mode);
        if (shouldShow && outlineTexture == null)
            RebuildOutline();

        if (immediate)
        {
            currentAlpha = shouldShow && outlineTexture != null ? AnimatedAlpha() : 0f;
            hoverBlend = 0f;
            isHovering = false;
        }

        ApplyAlpha();
    }

    bool ShouldShow(SkillModeType mode)
    {
        if (SkillInteractionLock.IsLocked)
            return false;

        if (forceVisibleForPreview)
            return true;

        if (mode != SkillModeType.Assemble)
            return false;

        ResolveAssemblyGate();
        return assemblyGate != null && assemblyGate.IsReadyForAssemble;
    }

    float AnimatedAlpha()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) * 0.5f;
        return Mathf.Clamp01(IdleAlpha + pulse * PulseAlpha);
    }

    void ResolveRoot()
    {
        if (outlineRoot == null)
            outlineRoot = transform as RectTransform;

        rootRect = outlineRoot != null ? outlineRoot : transform as RectTransform;
    }

    void ResolveAssemblyGate()
    {
        if (assemblyGate != null)
            return;

        assemblyGate = GetComponent<ShowCompletedAssembly>();
        if (assemblyGate == null)
            assemblyGate = GetComponentInParent<ShowCompletedAssembly>();
        if (assemblyGate == null)
            assemblyGate = GetComponentInChildren<ShowCompletedAssembly>(true);
        if (assemblyGate == null && rootRect != null)
            assemblyGate = rootRect.GetComponentInChildren<ShowCompletedAssembly>(true);
    }

    void EnsureOutlineObject()
    {
        if (rootRect == null)
            return;

        if (outlineImage != null && outlineRect != null && hoverImage != null && hoverRect != null)
            return;

        Transform existing = rootRect.Find(OutlineObjectName);
        GameObject outlineObject = existing != null
            ? existing.gameObject
            : new GameObject(OutlineObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Canvas), typeof(RawImage));

        if (existing == null)
            outlineObject.transform.SetParent(rootRect, false);

        outlineRect = outlineObject.transform as RectTransform;
        outlineCanvas = outlineObject.GetComponent<Canvas>();
        outlineImage = outlineObject.GetComponent<RawImage>();

        if (outlineCanvas == null)
            outlineCanvas = outlineObject.AddComponent<Canvas>();

        if (outlineImage == null)
            outlineImage = outlineObject.AddComponent<RawImage>();

        outlineImage.raycastTarget = false;
        outlineImage.maskable = false;
        outlineImage.texture = outlineTexture;
        outlineImage.enabled = false;

        EnsureHoverImage();
        outlineObject.transform.SetAsLastSibling();
        SyncSortingCanvas();
    }

    void EnsureHoverImage()
    {
        if (outlineRect == null)
            return;

        Transform existing = outlineRect.Find(HoverObjectName);
        GameObject hoverObject = existing != null
            ? existing.gameObject
            : new GameObject(HoverObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));

        if (existing == null)
            hoverObject.transform.SetParent(outlineRect, false);

        hoverRect = hoverObject.transform as RectTransform;
        hoverImage = hoverObject.GetComponent<RawImage>();
        hoverImage.raycastTarget = false;
        hoverImage.maskable = false;
        hoverImage.texture = hoverTexture;
        hoverImage.enabled = false;

        hoverRect.anchorMin = Vector2.zero;
        hoverRect.anchorMax = Vector2.one;
        hoverRect.offsetMin = Vector2.zero;
        hoverRect.offsetMax = Vector2.zero;
        hoverRect.pivot = new Vector2(0.5f, 0.5f);
        hoverRect.localRotation = Quaternion.identity;
        hoverRect.localScale = Vector3.one;
        hoverRect.SetAsLastSibling();
    }

    void SyncSortingCanvas()
    {
        if (outlineCanvas == null || rootRect == null)
            return;

        Canvas sourceCanvas = rootRect.GetComponentInParent<Canvas>();
        if (sourceCanvas != null && sourceCanvas != outlineCanvas)
        {
            outlineCanvas.renderMode = sourceCanvas.renderMode;
            outlineCanvas.worldCamera = sourceCanvas.worldCamera;
            outlineCanvas.planeDistance = sourceCanvas.planeDistance;
            outlineCanvas.pixelPerfect = sourceCanvas.pixelPerfect;
            outlineCanvas.sortingLayerID = sourceCanvas.sortingLayerID;
            outlineCanvas.additionalShaderChannels = sourceCanvas.additionalShaderChannels;
            outlineCanvas.sortingOrder = sourceCanvas.sortingOrder + SortingOrderOffset;
        }
        else
        {
            outlineCanvas.sortingOrder = SortingOrderOffset;
        }

        outlineCanvas.overrideSorting = true;
    }

    void ApplyAlpha()
    {
        bool baseVisible = false;
        bool hoverVisible = false;

        if (outlineImage != null)
        {
            Color color = assembleColor;
            color.a *= currentAlpha;

            outlineImage.color = color;
            outlineImage.texture = outlineTexture;
            baseVisible = outlineTexture != null && color.a > 0.001f;
            outlineImage.enabled = baseVisible;
        }

        if (hoverImage != null)
        {
            Color hoverColor = assembleColor;
            hoverColor.a *= Mathf.Clamp01(currentAlpha * hoverBlend);

            hoverImage.color = hoverColor;
            hoverImage.texture = hoverTexture;
            hoverVisible = hoverTexture != null && hoverColor.a > 0.001f;
            hoverImage.enabled = hoverVisible;
        }

        if (outlineCanvas != null)
            outlineCanvas.enabled = baseVisible || hoverVisible;
    }

    void UpdateHoverState()
    {
        if (outlineTexture == null)
        {
            isHovering = false;
            return;
        }

        isHovering = IsPointerOverComposite();
    }

    public void PrepareOutline()
    {
        ResolveRoot();
        ResolveAssemblyGate();
        EnsureOutlineObject();

        if (rootRect == null)
            return;

        if (textureDirty || outlineTexture == null || hoverTexture == null || NeedsRebuild())
            RebuildOutline();

        currentAlpha = 0f;
        hoverBlend = 0f;
        isHovering = false;
        ApplyAlpha();
    }

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        ResolveRoot();
        EnsureOutlineObject();

        if (rootRect == null)
            return false;

        if (textureDirty || outlineTexture == null || hoverTexture == null || NeedsRebuild())
            RebuildOutline();

        if (sourceSamples.Count == 0)
            return false;

        Camera camera = EventCameraFor(rootRect);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPoint, camera, out Vector2 rootLocalPoint))
            return false;

        return SampleCompositeAlpha(rootLocalPoint.x, rootLocalPoint.y) >= AlphaThreshold;
    }

    bool NeedsRebuild()
    {
        if (outlineTexture == null || hoverTexture == null)
            return true;

        ResolveSourceImages();
        if (resolvedSources.Count != snapshots.Count)
            return true;

        for (int i = 0; i < resolvedSources.Count; i++)
        {
            Image image = resolvedSources[i];
            if (image == null)
                return true;

            RectTransform imageRect = image.rectTransform;
            SourceSnapshot snapshot = snapshots[i];
            if (snapshot.image != image
                || snapshot.sprite != image.sprite
                || snapshot.enabled != IsRenderableSource(image)
                || snapshot.color != image.color
                || imageRect.hasChanged)
            {
                return true;
            }
        }

        return false;
    }

    void RebuildOutline()
    {
        textureDirty = false;

        if (rootRect == null)
            return;

        ResolveSourceImages();
        BuildSourceSamples();

        if (sourceSamples.Count == 0 || !TryCalculateBounds(out Rect bounds))
        {
            ClearTextures();
            StoreSnapshots();
            return;
        }

        float pixelsPerUnit = Mathf.Max(0.1f, TexturePixelsPerUnit);
        int width = Mathf.CeilToInt(bounds.width * pixelsPerUnit);
        int height = Mathf.CeilToInt(bounds.height * pixelsPerUnit);

        if (width <= 0 || height <= 0)
        {
            ClearTextures();
            StoreSnapshots();
            return;
        }

        if (width > MaxTextureSize || height > MaxTextureSize)
        {
            float scale = Mathf.Min(MaxTextureSize / (float)width, MaxTextureSize / (float)height);
            pixelsPerUnit *= scale;
            width = Mathf.Max(1, Mathf.CeilToInt(bounds.width * pixelsPerUnit));
            height = Mathf.Max(1, Mathf.CeilToInt(bounds.height * pixelsPerUnit));
        }

        width = Mathf.Clamp(width, 1, MaxTextureSize);
        height = Mathf.Clamp(height, 1, MaxTextureSize);

        bool[] mask = RasterizeSourceMask(bounds, width, height, pixelsPerUnit);
        if (MergeGapPixels > 0)
            mask = CloseMask(mask, width, height, MergeGapPixels);

        Color32[] basePixels = BuildOutlinePixels(mask, width, height, BaseOutlinePixels, BaseGlowPixels, BaseGlowOpacity);
        Color32[] hoverPixels = BuildOutlinePixels(mask, width, height, HoverOutlinePixels, HoverGlowPixels, HoverGlowOpacity);

        EnsureTextures(width, height);
        outlineTexture.SetPixels32(basePixels);
        outlineTexture.Apply(false, false);
        hoverTexture.SetPixels32(hoverPixels);
        hoverTexture.Apply(false, false);

        outlineLocalBounds = bounds;
        SyncOutlineRect();
        StoreSnapshots();
    }

    void ClearTextures()
    {
        ReleaseTextures();
        if (outlineImage != null)
            outlineImage.texture = null;
        if (hoverImage != null)
            hoverImage.texture = null;
    }

    void ResolveSourceImages()
    {
        resolvedSources.Clear();

        if (sourceImages != null)
        {
            for (int i = 0; i < sourceImages.Length; i++)
                AddSource(sourceImages[i]);
        }

        if (!AutoFindPuzzlePartImages || rootRect == null)
            return;

        PuzzlePart[] parts = rootRect.GetComponentsInChildren<PuzzlePart>(IncludeInactivePuzzleParts);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null)
                continue;

            if (AutoFindOnlyArmParts && !IsArmPuzzlePart(parts[i]))
                continue;

            AddSource(parts[i].GetComponent<Image>());
        }
    }

    static bool IsArmPuzzlePart(PuzzlePart part)
    {
        if (part == null)
            return false;

        string id = part.partId != null ? part.partId.Trim().ToUpperInvariant() : string.Empty;
        if (id == "A" || id == "B" || id == "C" || id == "D")
            return true;

        string objectName = part.name.ToUpperInvariant();
        return objectName == "ARM1"
            || objectName == "ARM2"
            || objectName == "ARM3"
            || objectName == "ARM4";
    }

    void AddSource(Image image)
    {
        if (image == null || resolvedSources.Contains(image))
            return;

        resolvedSources.Add(image);
    }

    void BuildSourceSamples()
    {
        sourceSamples.Clear();

        for (int i = 0; i < resolvedSources.Count; i++)
        {
            Image image = resolvedSources[i];
            if (!IsRenderableSource(image))
                continue;

            Sprite sprite = image.sprite;
            Texture2D texture = sprite.texture;
            Rect textureRect;
            bool canSampleTexture = texture != null && texture.isReadable;

            try
            {
                textureRect = sprite.textureRect;
            }
            catch
            {
                textureRect = new Rect(0f, 0f, texture != null ? texture.width : 1f, texture != null ? texture.height : 1f);
                canSampleTexture = false;
            }

            if (!canSampleTexture && texture != null && unreadableTextures.Add(texture))
            {
                Debug.LogWarning(
                    $"{nameof(AssembleCompositeOutlineTarget)}: '{texture.name}' texture is not readable. Enable Read/Write to include it in the composite outline.",
                    this);
            }

            if (!canSampleTexture && SkipUnreadableTextures)
                continue;

            Rect drawRect = GetImageDrawRect(image);
            if (drawRect.width <= 0f || drawRect.height <= 0f)
                continue;

            if (ShouldSampleFullTextureCanvas(image, texture))
                textureRect = new Rect(0f, 0f, texture.width, texture.height);

            SourceSample sample = new SourceSample
            {
                rect = image.rectTransform,
                sprite = sprite,
                texture = texture,
                textureRect = textureRect,
                drawRect = drawRect,
                rootToImageMatrix = image.rectTransform.worldToLocalMatrix * rootRect.localToWorldMatrix,
                colorAlpha = image.color.a,
                canSampleTexture = canSampleTexture
            };

            sourceSamples.Add(sample);
        }
    }

    static bool IsRenderableSource(Image image)
    {
        return image != null
            && image.enabled
            && image.gameObject.activeInHierarchy
            && image.sprite != null;
    }

    bool TryCalculateBounds(out Rect bounds)
    {
        bounds = default;
        bool hasPoint = false;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < sourceSamples.Count; i++)
        {
            RectTransform rect = sourceSamples[i].rect;
            if (rect == null)
                continue;

            rect.GetWorldCorners(corners);
            for (int j = 0; j < corners.Length; j++)
            {
                Vector2 localPoint = rootRect.InverseTransformPoint(corners[j]);
                if (!hasPoint)
                {
                    bounds = new Rect(localPoint, Vector2.zero);
                    hasPoint = true;
                    continue;
                }

                bounds.xMin = Mathf.Min(bounds.xMin, localPoint.x);
                bounds.xMax = Mathf.Max(bounds.xMax, localPoint.x);
                bounds.yMin = Mathf.Min(bounds.yMin, localPoint.y);
                bounds.yMax = Mathf.Max(bounds.yMax, localPoint.y);
            }
        }

        if (!hasPoint)
            return false;

        float outlinePadding = BoundsPadding + MergeGapPixels + HoverOutlinePixels + HoverGlowPixels + 2f;
        bounds.xMin -= outlinePadding;
        bounds.xMax += outlinePadding;
        bounds.yMin -= outlinePadding;
        bounds.yMax += outlinePadding;
        return bounds.width > 0f && bounds.height > 0f;
    }

    bool[] RasterizeSourceMask(Rect bounds, int width, int height, float pixelsPerUnit)
    {
        bool[] mask = new bool[width * height];
        float unitsPerPixel = 1f / pixelsPerUnit;

        for (int y = 0; y < height; y++)
        {
            float localY = bounds.yMin + (y + 0.5f) * unitsPerPixel;

            for (int x = 0; x < width; x++)
            {
                float localX = bounds.xMin + (x + 0.5f) * unitsPerPixel;
                if (SampleCompositeAlpha(localX, localY) >= AlphaThreshold)
                    mask[y * width + x] = true;
            }
        }

        return mask;
    }

    float SampleCompositeAlpha(float rootLocalX, float rootLocalY)
    {
        Vector3 rootLocalPoint = new Vector3(rootLocalX, rootLocalY, 0f);
        float bestAlpha = 0f;

        for (int i = 0; i < sourceSamples.Count; i++)
        {
            SourceSample sample = sourceSamples[i];
            Vector3 imageLocalPoint = sample.rootToImageMatrix.MultiplyPoint3x4(rootLocalPoint);

            if (!sample.drawRect.Contains(imageLocalPoint))
                continue;

            float u = Mathf.InverseLerp(sample.drawRect.xMin, sample.drawRect.xMax, imageLocalPoint.x);
            float v = Mathf.InverseLerp(sample.drawRect.yMin, sample.drawRect.yMax, imageLocalPoint.y);
            float alpha = sample.colorAlpha;

            if (sample.canSampleTexture && sample.texture != null)
            {
                float textureU = (sample.textureRect.x + sample.textureRect.width * u) / sample.texture.width;
                float textureV = (sample.textureRect.y + sample.textureRect.height * v) / sample.texture.height;
                alpha *= sample.texture.GetPixelBilinear(textureU, textureV).a;
            }

            if (alpha > bestAlpha)
                bestAlpha = alpha;

            if (bestAlpha >= AlphaThreshold)
                return bestAlpha;
        }

        return bestAlpha;
    }

    static Rect GetImageDrawRect(Image image)
    {
        Rect rect = image.rectTransform.rect;
        Sprite sprite = image.sprite;

        if (sprite == null || !image.preserveAspect || rect.width <= 0f || rect.height <= 0f)
            return rect;

        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;
        if (spriteWidth <= 0f || spriteHeight <= 0f)
            return rect;

        float spriteRatio = spriteWidth / spriteHeight;
        float rectRatio = rect.width / rect.height;
        Vector2 center = rect.center;

        if (spriteRatio > rectRatio)
        {
            float height = rect.width / spriteRatio;
            return new Rect(rect.xMin, center.y - height * 0.5f, rect.width, height);
        }

        float width = rect.height * spriteRatio;
        return new Rect(center.x - width * 0.5f, rect.yMin, width, rect.height);
    }

    bool ShouldSampleFullTextureCanvas(Image image, Texture2D texture)
    {
        if (!UseFullTextureWhenImageMatchesTexture || image == null || texture == null)
            return false;

        Rect rect = image.rectTransform.rect;
        return Mathf.Abs(rect.width - texture.width) <= TextureSizeMatchTolerance
            && Mathf.Abs(rect.height - texture.height) <= TextureSizeMatchTolerance;
    }

    void SyncOutlineRect()
    {
        if (outlineRect == null || rootRect == null)
            return;

        outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRect.pivot = new Vector2(0.5f, 0.5f);
        outlineRect.sizeDelta = outlineLocalBounds.size;
        outlineRect.anchoredPosition = outlineLocalBounds.center - rootRect.rect.center;
        outlineRect.localRotation = Quaternion.identity;
        outlineRect.localScale = Vector3.one;

        if (hoverRect != null)
        {
            hoverRect.anchorMin = Vector2.zero;
            hoverRect.anchorMax = Vector2.one;
            hoverRect.offsetMin = Vector2.zero;
            hoverRect.offsetMax = Vector2.zero;
            hoverRect.localRotation = Quaternion.identity;
            hoverRect.localScale = Vector3.one;
        }

        outlineRect.SetAsLastSibling();
    }

    bool IsPointerOverComposite()
    {
        return ContainsScreenPoint(Input.mousePosition);
    }

    static Camera EventCameraFor(RectTransform rect)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    void StoreSnapshots()
    {
        snapshots.Clear();
        for (int i = 0; i < resolvedSources.Count; i++)
        {
            Image image = resolvedSources[i];
            if (image == null)
                continue;

            snapshots.Add(new SourceSnapshot
            {
                image = image,
                sprite = image.sprite,
                color = image.color,
                enabled = IsRenderableSource(image)
            });

            image.rectTransform.hasChanged = false;
        }
    }

    void EnsureTextures(int width, int height)
    {
        if (outlineTexture == null || outlineTexture.width != width || outlineTexture.height != height)
        {
            ReleaseTexture(ref outlineTexture);
            outlineTexture = CreateTexture(width, height, OutlineObjectName + "_Texture");
        }

        if (hoverTexture == null || hoverTexture.width != width || hoverTexture.height != height)
        {
            ReleaseTexture(ref hoverTexture);
            hoverTexture = CreateTexture(width, height, HoverObjectName + "_Texture");
        }

        if (outlineImage != null)
            outlineImage.texture = outlineTexture;
        if (hoverImage != null)
            hoverImage.texture = hoverTexture;
    }

    static Texture2D CreateTexture(int width, int height, string textureName)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
    }

    void ReleaseTextures()
    {
        ReleaseTexture(ref outlineTexture);
        ReleaseTexture(ref hoverTexture);
    }

    static void ReleaseTexture(ref Texture2D texture)
    {
        if (texture == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(texture);
        else
#endif
            Destroy(texture);

        texture = null;
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

    static bool[] CloseMask(bool[] mask, int width, int height, int radius)
    {
        bool[] dilated = DilateMask(mask, width, height, radius);
        return ErodeMask(dilated, width, height, radius);
    }

    static bool[] DilateMask(bool[] source, int width, int height, int radius)
    {
        bool[] horizontal = new bool[source.Length];
        bool[] result = new bool[source.Length];

        for (int y = 0; y < height; y++)
        {
            int count = 0;
            for (int x = -radius; x < width; x++)
            {
                int addX = x + radius;
                if (addX >= 0 && addX < width && source[y * width + addX])
                    count++;

                int removeX = x - radius - 1;
                if (removeX >= 0 && removeX < width && source[y * width + removeX])
                    count--;

                if (x >= 0)
                    horizontal[y * width + x] = count > 0;
            }
        }

        for (int x = 0; x < width; x++)
        {
            int count = 0;
            for (int y = -radius; y < height; y++)
            {
                int addY = y + radius;
                if (addY >= 0 && addY < height && horizontal[addY * width + x])
                    count++;

                int removeY = y - radius - 1;
                if (removeY >= 0 && removeY < height && horizontal[removeY * width + x])
                    count--;

                if (y >= 0)
                    result[y * width + x] = count > 0;
            }
        }

        return result;
    }

    static bool[] ErodeMask(bool[] source, int width, int height, int radius)
    {
        bool[] horizontal = new bool[source.Length];
        bool[] result = new bool[source.Length];
        int window = radius * 2 + 1;

        for (int y = 0; y < height; y++)
        {
            int count = 0;
            for (int x = -radius; x < width; x++)
            {
                int addX = x + radius;
                if (addX >= 0 && addX < width && source[y * width + addX])
                    count++;

                int removeX = x - radius - 1;
                if (removeX >= 0 && removeX < width && source[y * width + removeX])
                    count--;

                if (x >= 0)
                    horizontal[y * width + x] = count == window;
            }
        }

        for (int x = 0; x < width; x++)
        {
            int count = 0;
            for (int y = -radius; y < height; y++)
            {
                int addY = y + radius;
                if (addY >= 0 && addY < height && horizontal[addY * width + x])
                    count++;

                int removeY = y - radius - 1;
                if (removeY >= 0 && removeY < height && horizontal[removeY * width + x])
                    count--;

                if (y >= 0)
                    result[y * width + x] = count == window;
            }
        }

        return result;
    }

    struct SourceSample
    {
        public RectTransform rect;
        public Sprite sprite;
        public Texture2D texture;
        public Rect textureRect;
        public Rect drawRect;
        public Matrix4x4 rootToImageMatrix;
        public float colorAlpha;
        public bool canSampleTexture;
    }

    struct SourceSnapshot
    {
        public Image image;
        public Sprite sprite;
        public Color color;
        public bool enabled;
    }
}
