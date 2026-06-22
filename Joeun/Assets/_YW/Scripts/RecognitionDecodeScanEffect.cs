using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RecognitionDecodeScanVisualMode
{
    ProceduralLine,
    SpriteFill,
    ProceduralLineAndSpriteFill
}

[DisallowMultipleComponent]
public class RecognitionDecodeScanEffect : MonoBehaviour
{
    const string EffectObjectName = "__RecognitionDecodeFx";
    const string FillScanObjectName = "__RecognitionDecodeFillScan";
    const string LetterObjectName = "__RecognitionDecodeLetter";
    const string LetterSpritesObjectName = "__RecognitionDecodeLetterSprites";
    const string LetterResourceFolder = "DecodeLetters";

    const float ScanDuration = 0.56f;
    const float LetterPopDuration = 0.34f;
    const float HoldScanNoiseDuration = 0.04f;
    const float FillScanActiveAlpha = 0.9f;
    const float FillScanResolvedAlpha = 0f;
    const float FillScanScale = 1f;
    const float FillScanVerticalScale = 1f;

    static readonly Color DecodeColor = new Color(1f, 0.12f, 0.82f, 1f);
    static readonly Color LetterColor = new Color(1f, 0.72f, 0.96f, 0.95f);

    [Header("Sprite Scan")]
    [SerializeField] RecognitionDecodeScanVisualMode scanVisualMode = RecognitionDecodeScanVisualMode.ProceduralLine;
    [SerializeField] UnityEngine.Object fillScanAsset = null;
    [SerializeField] Image scanAreaImage = null;
    [SerializeField, HideInInspector] RectTransform scanArea = null;
    [SerializeField, HideInInspector] Sprite fillScanSprite = null;

    RectTransform ownerRect;
    RectTransform effectRect;
    RectTransform fillScanRect;
    RectTransform letterRect;
    RectTransform letterSpritesRect;
    RecognitionDecodeScanGraphic scanGraphic;
    Image fillScanImage;
    Text letterText;
    Coroutine routine;
    readonly List<Image> letterSpriteImages = new List<Image>();
    Texture2D runtimeFillScanTexture;
    Sprite runtimeFillScanSprite;

    public bool IsPlaying => routine != null;

    void OnDestroy()
    {
        ClearRuntimeFillScanSprite();
    }

    public void SetFillScanAsset(UnityEngine.Object asset)
    {
        if (fillScanAsset == asset)
            return;

        fillScanAsset = asset;
        ClearRuntimeFillScanSprite();

        if (fillScanImage != null)
            ApplyFillScanImageSettings();
    }

    public void SetScanVisualMode(RecognitionDecodeScanVisualMode mode)
    {
        scanVisualMode = mode;
    }

    public void SetScanArea(RectTransform area)
    {
        scanArea = area;
        if (scanAreaImage != null && scanAreaImage.rectTransform != area)
            scanAreaImage = null;

        EnsureObjects();
    }

    public void SetScanImage(Image image)
    {
        scanAreaImage = image;
        scanArea = image != null ? image.rectTransform : null;
        EnsureObjects();
    }

    public void Prepare()
    {
        EnsureObjects();
        Hide();
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        EnsureObjects();
        if (scanGraphic == null || letterText == null)
            return;

        scanGraphic.enabled = false;
        HideFillScan();
        HideLetterSprites();
        letterText.enabled = false;
        letterText.text = string.Empty;
        letterRect.localScale = Vector3.one;
    }

    public void ShowResolved(char letter)
    {
        ShowResolved(letter == '\0' ? "?" : letter.ToString());
    }

    public void ShowResolved(string text)
    {
        EnsureObjects();
        if (scanGraphic == null || letterText == null)
            return;

        scanGraphic.enabled = false;
        HideFillScan();
        ApplyLetter(text, 1f, 1f);
    }

    public IEnumerator Play(char letter)
    {
        yield return Play(letter == '\0' ? "?" : letter.ToString());
    }

    public IEnumerator Play(string text)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PlayRoutine(text));
        yield return routine;
    }

    IEnumerator PlayRoutine(string text)
    {
        EnsureObjects();
        if (scanGraphic == null || letterText == null)
        {
            routine = null;
            yield break;
        }

        bool useProceduralScan = UsesProceduralScan();
        bool useFillScan = UsesFillScan() && HasFillScanSprite();
        scanGraphic.enabled = useProceduralScan;
        SetFillScan(0f, FillScanActiveAlpha, useFillScan);
        letterText.enabled = false;
        letterText.text = string.Empty;
        letterRect.localScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < ScanDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = EaseOutCubic(Mathf.Clamp01(elapsed / ScanDuration));

            if (useProceduralScan)
                scanGraphic.SetScan(DecodeColor, progress, Time.unscaledTime);

            SetFillScan(progress, FillScanActiveAlpha, useFillScan);
            yield return null;
        }

        if (HoldScanNoiseDuration > 0f)
            yield return new WaitForSecondsRealtime(HoldScanNoiseDuration);

        elapsed = 0f;
        while (elapsed < LetterPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / LetterPopDuration);
            float eased = EaseOutBack(progress);
            float alpha = Mathf.SmoothStep(0f, 1f, progress);
            float scale = Mathf.LerpUnclamped(0.72f, 1f, eased);

            if (useProceduralScan)
                scanGraphic.SetResolved(DecodeColor);

            SetFillScan(1f, Mathf.Lerp(FillScanActiveAlpha, FillScanResolvedAlpha, progress), useFillScan);
            ApplyLetter(text, alpha, scale);
            yield return null;
        }

        scanGraphic.enabled = false;
        HideFillScan();
        ApplyLetter(text, 1f, 1f);
        routine = null;
    }

    void EnsureObjects()
    {
        ownerRect = ResolveEffectRoot();
        if (ownerRect == null)
            return;

        if (effectRect == null || effectRect.parent != ownerRect)
        {
            Transform existing = ownerRect.Find(EffectObjectName);
            GameObject effectObject = existing != null
                ? existing.gameObject
                : effectRect != null
                    ? effectRect.gameObject
                    : new GameObject(EffectObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RecognitionDecodeScanGraphic));

            if (effectObject.transform.parent != ownerRect)
                effectObject.transform.SetParent(ownerRect, false);

            effectRect = effectObject.transform as RectTransform;
            scanGraphic = effectObject.GetComponent<RecognitionDecodeScanGraphic>();
            if (scanGraphic == null)
                scanGraphic = effectObject.AddComponent<RecognitionDecodeScanGraphic>();

            scanGraphic.raycastTarget = false;
            scanGraphic.maskable = false;
        }

        Stretch(effectRect);
        effectRect.gameObject.transform.SetAsLastSibling();

        EnsureFillScanImage();

        if (letterRect == null)
        {
            Transform existing = effectRect.Find(LetterObjectName);
            GameObject letterObject = existing != null
                ? existing.gameObject
                : new GameObject(LetterObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));

            if (existing == null)
                letterObject.transform.SetParent(effectRect, false);

            letterRect = letterObject.transform as RectTransform;
            Stretch(letterRect);
            letterObject.transform.SetAsLastSibling();

            letterText = letterObject.GetComponent<Text>();
            letterText.raycastTarget = false;
            letterText.alignment = TextAnchor.MiddleCenter;
            letterText.fontStyle = FontStyle.Bold;
            letterText.resizeTextForBestFit = true;
            letterText.resizeTextMinSize = 20;
            letterText.resizeTextMaxSize = 96;
            letterText.supportRichText = false;

            if (letterText.font == null)
            {
                letterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (letterText.font == null)
                    letterText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        EnsureLetterSpritesRoot();
    }

    RectTransform ResolveEffectRoot()
    {
        if (scanAreaImage != null)
            return scanAreaImage.rectTransform;

        if (scanArea != null)
            return scanArea;

        return transform as RectTransform;
    }

    void EnsureLetterSpritesRoot()
    {
        if (effectRect == null)
            return;

        if (letterSpritesRect != null)
            return;

        Transform existing = effectRect.Find(LetterSpritesObjectName);
        GameObject spritesObject = existing != null
            ? existing.gameObject
            : new GameObject(LetterSpritesObjectName, typeof(RectTransform));

        if (existing == null)
            spritesObject.transform.SetParent(effectRect, false);

        letterSpritesRect = spritesObject.transform as RectTransform;
        Stretch(letterSpritesRect);
        spritesObject.transform.SetAsLastSibling();
    }

    void EnsureFillScanImage()
    {
        if (effectRect == null)
            return;

        if (fillScanRect != null)
        {
            ApplyFillScanImageSettings();
            return;
        }

        Transform existing = effectRect.Find(FillScanObjectName);
        GameObject fillObject = existing != null
            ? existing.gameObject
            : new GameObject(FillScanObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            fillObject.transform.SetParent(effectRect, false);

        fillScanRect = fillObject.transform as RectTransform;
        StretchFillScanRect();
        fillScanImage = fillObject.GetComponent<Image>();
        ApplyFillScanImageSettings();
        fillObject.transform.SetAsFirstSibling();
    }

    void ApplyFillScanImageSettings()
    {
        if (fillScanRect == null)
            return;

        StretchFillScanRect();

        if (fillScanImage == null)
            fillScanImage = fillScanRect.GetComponent<Image>();

        if (fillScanImage == null)
            return;

        fillScanImage.raycastTarget = false;
        fillScanImage.maskable = false;
        fillScanImage.sprite = ResolveFillScanSprite();
        fillScanImage.type = Image.Type.Filled;
        fillScanImage.fillMethod = Image.FillMethod.Horizontal;
        fillScanImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillScanImage.fillAmount = 0f;
        fillScanImage.preserveAspect = false;
        fillScanImage.enabled = false;
    }

    bool HasFillScanSprite()
    {
        return ResolveFillScanSprite() != null && fillScanImage != null;
    }

    bool UsesProceduralScan()
    {
        return scanVisualMode == RecognitionDecodeScanVisualMode.ProceduralLine
            || scanVisualMode == RecognitionDecodeScanVisualMode.ProceduralLineAndSpriteFill;
    }

    bool UsesFillScan()
    {
        return scanVisualMode == RecognitionDecodeScanVisualMode.SpriteFill
            || scanVisualMode == RecognitionDecodeScanVisualMode.ProceduralLineAndSpriteFill;
    }

    void SetFillScan(float amount, float alpha, bool visible)
    {
        if (fillScanImage == null)
            return;

        Sprite sprite = ResolveFillScanSprite();
        fillScanImage.enabled = visible && sprite != null;
        if (!fillScanImage.enabled)
            return;

        fillScanImage.sprite = sprite;
        fillScanImage.fillAmount = Mathf.Clamp01(amount);

        Color color = Color.white;
        color.a = Mathf.Clamp01(alpha);
        fillScanImage.color = color;
    }

    void HideFillScan()
    {
        if (fillScanImage == null)
            return;

        fillScanImage.fillAmount = 0f;
        fillScanImage.enabled = false;
    }

    void StretchFillScanRect()
    {
        if (fillScanRect == null)
            return;

        Stretch(fillScanRect);
        fillScanRect.localScale = new Vector3(FillScanScale, FillScanVerticalScale, 1f);
    }

    Sprite ResolveFillScanSprite()
    {
        if (fillScanAsset is Sprite assetSprite)
            return assetSprite;

        if (fillScanAsset is Texture2D assetTexture)
            return GetOrCreateSprite(assetTexture);

        if (fillScanSprite != null)
            return fillScanSprite;

        return LoadSprite("PatternPuzzle/decoding2");
    }

    Sprite GetOrCreateSprite(Texture2D texture)
    {
        if (texture == null)
            return null;

        if (runtimeFillScanSprite != null && runtimeFillScanTexture == texture)
            return runtimeFillScanSprite;

        ClearRuntimeFillScanSprite();
        runtimeFillScanTexture = texture;
        runtimeFillScanSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return runtimeFillScanSprite;
    }

    void ClearRuntimeFillScanSprite()
    {
        if (runtimeFillScanSprite != null)
        {
            Destroy(runtimeFillScanSprite);
            runtimeFillScanSprite = null;
        }

        runtimeFillScanTexture = null;
    }

    void ApplyLetter(string text, float alpha, float scale)
    {
        string value = string.IsNullOrWhiteSpace(text) ? "?" : text.Trim().ToUpperInvariant();
        bool usedSprites = ApplyLetterSprites(value, alpha, scale);

        letterText.enabled = !usedSprites;
        letterText.text = value;
        Color color = LetterColor;
        color.a = alpha;
        letterText.color = color;
        letterRect.localScale = Vector3.one * scale;
    }

    bool ApplyLetterSprites(string value, float alpha, float scale)
    {
        EnsureLetterSpritesRoot();
        if (letterSpritesRect == null || string.IsNullOrWhiteSpace(value))
        {
            HideLetterSprites();
            return false;
        }

        List<Sprite> sprites = new List<Sprite>(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
                continue;

            Sprite sprite = LoadLetterSprite(value[i]);
            if (sprite == null)
            {
                HideLetterSprites();
                return false;
            }

            sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            HideLetterSprites();
            return false;
        }

        EnsureLetterSpriteImages(sprites.Count);

        Rect rect = letterSpritesRect.rect;
        float width = rect.width > 0f ? rect.width : 100f;
        float height = rect.height > 0f ? rect.height : 100f;
        float letterSize = Mathf.Min(height * 0.82f, width / sprites.Count * 0.9f);
        float spacing = letterSize * 0.05f;
        float totalWidth = sprites.Count * letterSize + (sprites.Count - 1) * spacing;
        float startX = -totalWidth * 0.5f + letterSize * 0.5f;
        Color color = LetterColor;
        color.a = Mathf.Clamp01(alpha);

        for (int i = 0; i < letterSpriteImages.Count; i++)
        {
            Image image = letterSpriteImages[i];
            bool active = i < sprites.Count;
            image.enabled = active;

            if (!active)
                continue;

            RectTransform imageRect = image.transform as RectTransform;
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = new Vector2(letterSize, letterSize);
            imageRect.anchoredPosition = new Vector2(startX + i * (letterSize + spacing), 0f);
            imageRect.localScale = Vector3.one;

            image.sprite = sprites[i];
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        letterSpritesRect.localScale = Vector3.one * scale;
        letterSpritesRect.gameObject.SetActive(true);
        letterSpritesRect.SetAsLastSibling();
        return true;
    }

    void EnsureLetterSpriteImages(int count)
    {
        EnsureLetterSpritesRoot();
        if (letterSpritesRect == null)
            return;

        while (letterSpriteImages.Count < count)
        {
            GameObject imageObject = new GameObject($"Letter_{letterSpriteImages.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(letterSpritesRect, false);

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            letterSpriteImages.Add(image);
        }
    }

    void HideLetterSprites()
    {
        if (letterSpritesRect != null)
        {
            letterSpritesRect.localScale = Vector3.one;
            letterSpritesRect.gameObject.SetActive(false);
        }

        for (int i = 0; i < letterSpriteImages.Count; i++)
        {
            if (letterSpriteImages[i] != null)
                letterSpriteImages[i].enabled = false;
        }
    }

    static Sprite LoadLetterSprite(char letter)
    {
        if (letter == '#')
            return LoadSprite($"{LetterResourceFolder}/letter_censored");

        if (!char.IsLetter(letter))
            return null;

        return LoadSprite($"{LetterResourceFolder}/letter_{char.ToUpperInvariant(letter)}");
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static float EaseOutBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t = value - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    static float EaseOutCubic(float value)
    {
        float t = 1f - value;
        return 1f - t * t * t;
    }

    static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }
}

public class RecognitionDecodeScanGraphic : MaskableGraphic
{
    const int RoundedCornerSegments = 5;

    Color effectColor = Color.magenta;
    float scanProgress;
    float noisePhase;
    float resolvedAlpha;
    bool showScanLine = true;
    bool resolved;

    public void SetScan(Color color, float progress, float phase, bool drawScanLine = true)
    {
        effectColor = color;
        scanProgress = Mathf.Clamp01(progress);
        noisePhase = phase;
        showScanLine = drawScanLine;
        resolved = false;
        resolvedAlpha = 1f;
        SetVerticesDirty();
    }

    public void SetResolved(Color color)
    {
        effectColor = color;
        scanProgress = 1f;
        resolved = true;
        resolvedAlpha = 1f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * 7.5f) + 1f) * 0.5f;
        float activeAlpha = resolved ? 0.28f * resolvedAlpha : 0.58f;

        Color fill = effectColor;
        fill.a = resolved ? 0.08f : 0.13f;
        DrawCellField(vh, rect, fill, resolved ? 0.45f + pulse * 0.08f : 0.78f);

        Rect[] cells = GetCellRects(rect);
        for (int i = 0; i < cells.Length; i++)
            DrawScanNoise(vh, cells[i], activeAlpha);

        DrawCenterReticle(vh, rect, resolved ? 0.12f : 0.26f);

        if (!resolved)
        {
            if (!showScanLine)
                return;

            DrawScanLine(vh, rect, scanProgress);
        }
    }

    void DrawCenterReticle(VertexHelper vh, Rect rect, float alpha)
    {
        Color line = effectColor;
        line.a = alpha;

        float midX = (rect.xMin + rect.xMax) * 0.5f;
        float midY = (rect.yMin + rect.yMax) * 0.5f;
        float gapX = Mathf.Clamp(rect.width * 0.045f, 5f, 12f);
        float gapY = Mathf.Clamp(rect.height * 0.045f, 5f, 12f);
        AddQuad(vh, rect.xMin, midY - 0.5f, rect.xMax, midY + 0.5f, line);
        AddQuad(vh, midX - 0.5f, rect.yMin, midX + 0.5f, rect.yMax, line);

        Color gap = effectColor;
        gap.a = alpha * 0.12f;
        AddQuad(vh, midX - gapX * 0.5f, rect.yMin, midX + gapX * 0.5f, rect.yMax, gap);
        AddQuad(vh, rect.xMin, midY - gapY * 0.5f, rect.xMax, midY + gapY * 0.5f, gap);
    }

    void DrawScanNoise(VertexHelper vh, Rect rect, float alpha)
    {
        Color horizontal = Color.Lerp(Color.white, effectColor, 0.6f);
        horizontal.a = alpha * 0.22f;

        float yOffset = Mathf.Repeat(noisePhase * 24f, 7f);
        for (float y = rect.yMin + yOffset; y < rect.yMax; y += 7f)
            AddQuad(vh, rect.xMin, y, rect.xMax, Mathf.Min(y + 1f, rect.yMax), horizontal);

        Color vertical = effectColor;
        vertical.a = alpha * 0.12f;

        float xOffset = Mathf.Repeat(noisePhase * 18f, 13f);
        for (float x = rect.xMin + xOffset; x < rect.xMax; x += 13f)
            AddQuad(vh, x, rect.yMin, Mathf.Min(x + 1f, rect.xMax), rect.yMax, vertical);

        Color tick = Color.white;
        tick.a = alpha * 0.22f;
        for (int i = 0; i < 6; i++)
        {
            float t = Mathf.Repeat(noisePhase * (0.37f + i * 0.09f) + i * 0.173f, 1f);
            float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Repeat(t * 1.71f + i * 0.23f, 1f));
            AddQuad(vh, x, y, Mathf.Min(x + rect.width * 0.16f, rect.xMax), Mathf.Min(y + 1.5f, rect.yMax), tick);
        }
    }

    void DrawScanLine(VertexHelper vh, Rect rect, float progress)
    {
        float width = Mathf.Min(Mathf.Clamp(rect.width * 0.28f, 28f, 58f), rect.width);
        float halfWidth = width * 0.5f;
        float center = Mathf.Lerp(rect.xMin - halfWidth, rect.xMax + halfWidth, progress);

        Color core = Color.white;
        core.a = 0.72f;

        int slices = Mathf.Max(8, Mathf.CeilToInt(rect.height / 9f));
        for (int i = 0; i < slices; i++)
        {
            float from = (float)i / slices;
            float to = (float)(i + 1) / slices;
            float middle = (from + to) * 0.5f;
            float edge = Mathf.Pow(Mathf.Abs(middle * 2f - 1f), 1.15f);
            float sliceWidth = width * Mathf.Lerp(0.58f, 1f, edge);
            float sliceHalf = sliceWidth * 0.5f;
            float y0 = Mathf.Lerp(rect.yMin, rect.yMax, from);
            float y1 = Mathf.Lerp(rect.yMin, rect.yMax, to);

            float x0 = center - sliceHalf;
            float x1 = center + sliceHalf;
            float leftSoft = Mathf.Lerp(x0, center, 0.48f);
            float rightSoft = Mathf.Lerp(center, x1, 0.52f);

            AddClippedGradientQuad(vh, rect, x0, y0, leftSoft, y1, WithAlpha(effectColor, 0f), WithAlpha(effectColor, 0.32f));
            AddClippedGradientQuad(vh, rect, leftSoft, y0, center, y1, WithAlpha(effectColor, 0.32f), WithAlpha(Color.white, 0.9f));
            AddClippedGradientQuad(vh, rect, center, y0, rightSoft, y1, WithAlpha(Color.white, 0.9f), WithAlpha(effectColor, 0.32f));
            AddClippedGradientQuad(vh, rect, rightSoft, y0, x1, y1, WithAlpha(effectColor, 0.32f), WithAlpha(effectColor, 0f));

            float coreHalf = Mathf.Lerp(0.75f, 1.45f, edge);
            AddClippedQuad(vh, rect, center - coreHalf, y0, center + coreHalf, y1, core);
        }
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    static Rect Expand(Rect rect, float amount)
    {
        rect.xMin -= amount;
        rect.xMax += amount;
        rect.yMin -= amount;
        rect.yMax += amount;
        return rect;
    }

    static Rect Inset(Rect rect, float amount)
    {
        rect.xMin += amount;
        rect.xMax -= amount;
        rect.yMin += amount;
        rect.yMax -= amount;
        return rect;
    }

    void DrawCellField(VertexHelper vh, Rect rect, Color fill, float borderAlpha)
    {
        Rect[] cells = GetCellRects(rect);
        for (int i = 0; i < cells.Length; i++)
        {
            Rect cell = cells[i];
            float radius = Mathf.Clamp(Mathf.Min(cell.width, cell.height) * 0.18f, 8f, 20f);
            AddRoundedRect(vh, cell, radius, fill);

            Color glow = effectColor;
            glow.a = borderAlpha * 0.18f;
            AddRoundedRing(vh, Expand(cell, 2.2f), Inset(cell, 6.5f), radius + 2.2f, Mathf.Max(0f, radius - 6.5f), glow);

            Color border = Color.Lerp(Color.white, effectColor, 0.48f);
            border.a = borderAlpha;
            AddRoundedRing(vh, cell, Inset(cell, 2.2f), radius, Mathf.Max(0f, radius - 2.2f), border);
        }
    }

    static Rect[] GetCellRects(Rect rect)
    {
        float midX = (rect.xMin + rect.xMax) * 0.5f;
        float midY = (rect.yMin + rect.yMax) * 0.5f;
        float gapX = Mathf.Clamp(rect.width * 0.045f, 5f, 12f);
        float gapY = Mathf.Clamp(rect.height * 0.045f, 5f, 12f);
        float pad = Mathf.Clamp(Mathf.Min(rect.width, rect.height) * 0.015f, 2f, 5f);

        return new[]
        {
            Inset(new Rect(rect.xMin, midY + gapY * 0.5f, midX - gapX * 0.5f - rect.xMin, rect.yMax - (midY + gapY * 0.5f)), pad),
            Inset(new Rect(midX + gapX * 0.5f, midY + gapY * 0.5f, rect.xMax - (midX + gapX * 0.5f), rect.yMax - (midY + gapY * 0.5f)), pad),
            Inset(new Rect(rect.xMin, rect.yMin, midX - gapX * 0.5f - rect.xMin, midY - gapY * 0.5f - rect.yMin), pad),
            Inset(new Rect(midX + gapX * 0.5f, rect.yMin, rect.xMax - (midX + gapX * 0.5f), midY - gapY * 0.5f - rect.yMin), pad)
        };
    }

    static void AddRing(VertexHelper vh, Rect outer, Rect inner, Color color)
    {
        AddQuad(vh, outer.xMin, inner.yMax, outer.xMax, outer.yMax, color);
        AddQuad(vh, outer.xMin, outer.yMin, outer.xMax, inner.yMin, color);
        AddQuad(vh, outer.xMin, inner.yMin, inner.xMin, inner.yMax, color);
        AddQuad(vh, inner.xMax, inner.yMin, outer.xMax, inner.yMax, color);
    }

    static void AddRoundedRect(VertexHelper vh, Rect rect, float radius, Color color)
    {
        if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
            return;

        List<Vector2> points = BuildRoundedRectPoints(rect, radius);
        if (points.Count < 3)
            return;

        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = rect.center;
        vh.AddVert(vertex);

        for (int i = 0; i < points.Count; i++)
        {
            vertex.position = points[i];
            vh.AddVert(vertex);
        }

        for (int i = 0; i < points.Count; i++)
        {
            int current = start + 1 + i;
            int next = start + 1 + ((i + 1) % points.Count);
            vh.AddTriangle(start, current, next);
        }
    }

    static void AddRoundedRing(VertexHelper vh, Rect outer, Rect inner, float outerRadius, float innerRadius, Color color)
    {
        if (outer.width <= 0f || outer.height <= 0f || inner.width <= 0f || inner.height <= 0f || color.a <= 0f)
            return;

        List<Vector2> outerPoints = BuildRoundedRectPoints(outer, outerRadius);
        List<Vector2> innerPoints = BuildRoundedRectPoints(inner, innerRadius);
        int count = Mathf.Min(outerPoints.Count, innerPoints.Count);
        if (count < 3)
            return;

        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        for (int i = 0; i < count; i++)
        {
            vertex.position = outerPoints[i];
            vh.AddVert(vertex);
            vertex.position = innerPoints[i];
            vh.AddVert(vertex);
        }

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int outerA = start + i * 2;
            int innerA = outerA + 1;
            int outerB = start + next * 2;
            int innerB = outerB + 1;

            vh.AddTriangle(outerA, outerB, innerB);
            vh.AddTriangle(innerB, innerA, outerA);
        }
    }

    static List<Vector2> BuildRoundedRectPoints(Rect rect, float radius)
    {
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        List<Vector2> points = new List<Vector2>((RoundedCornerSegments + 1) * 4);

        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);

        return points;
    }

    static void AddCorner(List<Vector2> points, Vector2 center, float radius, float fromDegrees, float toDegrees)
    {
        for (int i = 0; i <= RoundedCornerSegments; i++)
        {
            float t = (float)i / RoundedCornerSegments;
            float angle = Mathf.Lerp(fromDegrees, toDegrees, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Color color)
    {
        if (xMax <= xMin || yMax <= yMin || color.a <= 0f)
            return;

        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(xMin, yMin);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMin, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMin);
        vh.AddVert(vertex);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    static void AddClippedQuad(VertexHelper vh, Rect clip, float xMin, float yMin, float xMax, float yMax, Color color)
    {
        xMin = Mathf.Max(xMin, clip.xMin);
        xMax = Mathf.Min(xMax, clip.xMax);
        yMin = Mathf.Max(yMin, clip.yMin);
        yMax = Mathf.Min(yMax, clip.yMax);

        AddQuad(vh, xMin, yMin, xMax, yMax, color);
    }

    static void AddGradientQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Color left, Color right)
    {
        if (xMax <= xMin || yMax <= yMin)
            return;

        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;

        vertex.color = left;
        vertex.position = new Vector3(xMin, yMin);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMin, yMax);
        vh.AddVert(vertex);

        vertex.color = right;
        vertex.position = new Vector3(xMax, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMin);
        vh.AddVert(vertex);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    static void AddClippedGradientQuad(VertexHelper vh, Rect clip, float xMin, float yMin, float xMax, float yMax, Color left, Color right)
    {
        if (xMax <= xMin || yMax <= yMin)
            return;

        float originalXMin = xMin;
        float originalXMax = xMax;

        float clippedXMin = Mathf.Max(xMin, clip.xMin);
        float clippedXMax = Mathf.Min(xMax, clip.xMax);
        float clippedYMin = Mathf.Max(yMin, clip.yMin);
        float clippedYMax = Mathf.Min(yMax, clip.yMax);

        if (clippedXMax <= clippedXMin || clippedYMax <= clippedYMin)
            return;

        float leftT = Mathf.InverseLerp(originalXMin, originalXMax, clippedXMin);
        float rightT = Mathf.InverseLerp(originalXMin, originalXMax, clippedXMax);
        Color clippedLeft = Color.Lerp(left, right, leftT);
        Color clippedRight = Color.Lerp(left, right, rightT);

        AddGradientQuad(vh, clippedXMin, clippedYMin, clippedXMax, clippedYMax, clippedLeft, clippedRight);
    }
}
