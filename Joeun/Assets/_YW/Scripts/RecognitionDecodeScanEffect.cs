using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecognitionDecodeScanEffect : MonoBehaviour
{
    const string EffectObjectName = "__RecognitionDecodeFx";
    const string LetterObjectName = "__RecognitionDecodeLetter";

    const float ScanDuration = 0.56f;
    const float LetterPopDuration = 0.34f;
    const float HoldScanNoiseDuration = 0.04f;

    static readonly Color DecodeColor = new Color(1f, 0.12f, 0.82f, 1f);
    static readonly Color LetterColor = new Color(0.98f, 1f, 1f, 1f);

    RectTransform ownerRect;
    RectTransform effectRect;
    RectTransform letterRect;
    RecognitionDecodeScanGraphic scanGraphic;
    Text letterText;
    Coroutine routine;

    public bool IsPlaying => routine != null;

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

        scanGraphic.enabled = true;
        scanGraphic.SetResolved(DecodeColor);
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

        scanGraphic.enabled = true;
        letterText.enabled = false;
        letterText.text = string.Empty;
        letterRect.localScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < ScanDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = EaseOutCubic(Mathf.Clamp01(elapsed / ScanDuration));
            scanGraphic.SetScan(DecodeColor, progress, Time.unscaledTime);
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

            scanGraphic.SetResolved(DecodeColor);
            ApplyLetter(text, alpha, scale);
            yield return null;
        }

        scanGraphic.SetResolved(DecodeColor);
        ApplyLetter(text, 1f, 1f);
        routine = null;
    }

    void EnsureObjects()
    {
        ownerRect = transform as RectTransform;
        if (ownerRect == null)
            return;

        if (effectRect == null)
        {
            Transform existing = transform.Find(EffectObjectName);
            GameObject effectObject = existing != null
                ? existing.gameObject
                : new GameObject(EffectObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RecognitionDecodeScanGraphic));

            if (existing == null)
                effectObject.transform.SetParent(transform, false);

            effectRect = effectObject.transform as RectTransform;
            Stretch(effectRect);
            effectObject.transform.SetAsLastSibling();
            scanGraphic = effectObject.GetComponent<RecognitionDecodeScanGraphic>();
            scanGraphic.raycastTarget = false;
            scanGraphic.maskable = false;
        }

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
    }

    void ApplyLetter(string text, float alpha, float scale)
    {
        letterText.enabled = true;
        letterText.text = string.IsNullOrWhiteSpace(text) ? "?" : text.Trim().ToUpperInvariant();
        Color color = LetterColor;
        color.a = alpha;
        letterText.color = color;
        letterRect.localScale = Vector3.one * scale;
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
}

public class RecognitionDecodeScanGraphic : MaskableGraphic
{
    Color effectColor = Color.magenta;
    float scanProgress;
    float noisePhase;
    float resolvedAlpha;
    bool resolved;

    public void SetScan(Color color, float progress, float phase)
    {
        effectColor = color;
        scanProgress = Mathf.Clamp01(progress);
        noisePhase = phase;
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
        AddQuad(vh, rect.xMin, rect.yMin, rect.xMax, rect.yMax, fill);

        DrawScanNoise(vh, rect, activeAlpha);
        DrawCenterReticle(vh, rect, resolved ? 0.18f : 0.46f);
        DrawBorder(vh, rect, resolved ? 0.45f + pulse * 0.08f : 0.78f);

        if (!resolved)
            DrawScanLine(vh, rect, scanProgress);
    }

    void DrawBorder(VertexHelper vh, Rect rect, float alpha)
    {
        Color glow = effectColor;
        glow.a = alpha * 0.22f;
        AddRing(vh, rect, Inset(rect, 7f), glow);

        Color border = Color.Lerp(Color.white, effectColor, 0.48f);
        border.a = alpha;
        AddRing(vh, rect, Inset(rect, 2.4f), border);
    }

    void DrawCenterReticle(VertexHelper vh, Rect rect, float alpha)
    {
        Color line = effectColor;
        line.a = alpha;

        float midX = (rect.xMin + rect.xMax) * 0.5f;
        float midY = (rect.yMin + rect.yMax) * 0.5f;
        AddQuad(vh, rect.xMin, midY - 0.65f, rect.xMax, midY + 0.65f, line);
        AddQuad(vh, midX - 0.65f, rect.yMin, midX + 0.65f, rect.yMax, line);
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

    static void AddRing(VertexHelper vh, Rect outer, Rect inner, Color color)
    {
        AddQuad(vh, outer.xMin, inner.yMax, outer.xMax, outer.yMax, color);
        AddQuad(vh, outer.xMin, outer.yMin, outer.xMax, inner.yMin, color);
        AddQuad(vh, outer.xMin, inner.yMin, inner.xMin, inner.yMax, color);
        AddQuad(vh, inner.xMax, inner.yMin, outer.xMax, inner.yMax, color);
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
