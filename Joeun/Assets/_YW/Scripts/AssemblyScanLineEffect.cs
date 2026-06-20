using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays one cyan scanner sweep clipped by the completed assembly sprite alpha.
/// The generated overlay duplicates the completed Image sprite, so transparent
/// pixels stay transparent without a custom shader.
/// </summary>
[DisallowMultipleComponent]
public class AssemblyScanLineEffect : MonoBehaviour
{
    [SerializeField] private RectTransform scanArea;
    [SerializeField] private float duration = 0.54f;

    private const string GeneratedClipName = "__AssemblyScanClip";
    private const string GeneratedOverlayName = "__AssemblyScanSprite";
    private const float EdgePadding = 80f;
    private const float MaxAlpha = 0.82f;
    private const float ScanWidth = 220f;
    private const int SortingOrderOffset = 145;

    private Image sourceImage;
    private RectTransform generatedClip;
    private RectTransform generatedOverlay;
    private Image generatedOverlayImage;
    private Canvas sortingCanvas;
    private readonly Vector3[] areaCorners = new Vector3[4];

    private void Reset()
    {
        if (transform is RectTransform rect && HasDirectImage(rect))
            scanArea = rect;
    }

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    public void SetScanArea(RectTransform area)
    {
        if (area == null)
            return;

        scanArea = area;
        sourceImage = FindSourceImage(scanArea);
    }

    public IEnumerator Play()
    {
        ResolveReferences();

        if (scanArea == null || sourceImage == null || sourceImage.sprite == null)
            yield break;

        EnsureGeneratedObjects();
        if (generatedClip == null || generatedOverlay == null || generatedOverlayImage == null)
            yield break;

        SyncGeneratedClipToScanArea();
        SyncOverlayImage();
        EnsureSortingCanvas();
        DisableRaycastTargets();

        Rect areaInParent = CalculateAreaInParent(generatedClip.parent as RectTransform);
        float centerY = areaInParent.center.y;
        float startX = areaInParent.xMin - EdgePadding;
        float endX = areaInParent.xMax + EdgePadding;

        generatedClip.gameObject.SetActive(true);
        generatedClip.SetAsLastSibling();

        if (duration <= 0f)
        {
            SetClipPosition(endX, centerY);
            Hide();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Sin(t * Mathf.PI) * MaxAlpha;

            SetClipPosition(Mathf.Lerp(startX, endX, eased), centerY);
            AlignOverlayToSource();
            SetAlpha(alpha);
            yield return null;
        }

        Hide();
    }

    public void Hide()
    {
        SetAlpha(0f);

        if (generatedClip != null)
            generatedClip.gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (scanArea == null && transform is RectTransform selfRect && HasDirectImage(selfRect))
            scanArea = selfRect;

        if (scanArea != null && sourceImage == null)
            sourceImage = FindSourceImage(scanArea);
    }

    private void EnsureGeneratedObjects()
    {
        RectTransform parent = ResolveGeneratedParent();
        if (parent == null)
            return;

        Transform existingClip = parent.Find(GeneratedClipName);
        GameObject clipObject = existingClip != null
            ? existingClip.gameObject
            : new GameObject(GeneratedClipName, typeof(RectTransform), typeof(Canvas), typeof(RectMask2D));

        if (existingClip == null)
            clipObject.transform.SetParent(parent, false);

        generatedClip = clipObject.transform as RectTransform;
        generatedClip.anchorMin = new Vector2(0.5f, 0.5f);
        generatedClip.anchorMax = new Vector2(0.5f, 0.5f);
        generatedClip.pivot = new Vector2(0.5f, 0.5f);
        generatedClip.localRotation = Quaternion.identity;
        generatedClip.localScale = Vector3.one;

        Transform existingOverlay = generatedClip.Find(GeneratedOverlayName);
        GameObject overlayObject = existingOverlay != null
            ? existingOverlay.gameObject
            : new GameObject(GeneratedOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existingOverlay == null)
            overlayObject.transform.SetParent(generatedClip, false);

        generatedOverlay = overlayObject.transform as RectTransform;
        generatedOverlayImage = overlayObject.GetComponent<Image>();
        generatedOverlayImage.raycastTarget = false;
        generatedOverlayImage.maskable = true;
    }

    private RectTransform ResolveGeneratedParent()
    {
        if (scanArea != null && scanArea.parent is RectTransform parent)
            return parent;

        return transform as RectTransform;
    }

    private void SyncGeneratedClipToScanArea()
    {
        if (generatedClip == null || scanArea == null)
            return;

        Rect areaInParent = CalculateAreaInParent(generatedClip.parent as RectTransform);
        generatedClip.sizeDelta = new Vector2(ScanWidth, areaInParent.height + EdgePadding * 2f);
        generatedClip.localPosition = new Vector3(
            areaInParent.xMin - EdgePadding,
            areaInParent.center.y,
            generatedClip.localPosition.z);
    }

    private void SyncOverlayImage()
    {
        if (sourceImage == null || generatedOverlayImage == null || generatedOverlay == null)
            return;

        generatedOverlayImage.sprite = sourceImage.sprite;
        generatedOverlayImage.type = sourceImage.type;
        generatedOverlayImage.preserveAspect = sourceImage.preserveAspect;
        generatedOverlayImage.fillCenter = sourceImage.fillCenter;
        generatedOverlayImage.fillMethod = sourceImage.fillMethod;
        generatedOverlayImage.fillOrigin = sourceImage.fillOrigin;
        generatedOverlayImage.fillAmount = sourceImage.fillAmount;
        generatedOverlayImage.fillClockwise = sourceImage.fillClockwise;
        generatedOverlayImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;

        generatedOverlay.anchorMin = new Vector2(0.5f, 0.5f);
        generatedOverlay.anchorMax = new Vector2(0.5f, 0.5f);
        generatedOverlay.pivot = scanArea.pivot;
        generatedOverlay.sizeDelta = scanArea.rect.size;
        AlignOverlayToSource();
    }

    private void AlignOverlayToSource()
    {
        if (generatedOverlay == null || generatedClip == null || scanArea == null)
            return;

        generatedOverlay.localPosition = generatedClip.InverseTransformPoint(scanArea.position);
        generatedOverlay.localRotation = Quaternion.Inverse(generatedClip.rotation) * scanArea.rotation;
        generatedOverlay.localScale = DivideLossyScale(scanArea.lossyScale, generatedClip.lossyScale);
        generatedOverlay.sizeDelta = scanArea.rect.size;
    }

    private void EnsureSortingCanvas()
    {
        if (generatedClip == null)
            return;

        if (sortingCanvas == null || sortingCanvas.transform != generatedClip)
            sortingCanvas = generatedClip.GetComponent<Canvas>();
        if (sortingCanvas == null)
            sortingCanvas = generatedClip.gameObject.AddComponent<Canvas>();

        Canvas sourceCanvas = FindParentCanvas(generatedClip.parent);
        if (sourceCanvas != null)
        {
            sortingCanvas.renderMode = sourceCanvas.renderMode;
            sortingCanvas.worldCamera = sourceCanvas.worldCamera;
            sortingCanvas.planeDistance = sourceCanvas.planeDistance;
            sortingCanvas.pixelPerfect = sourceCanvas.pixelPerfect;
            sortingCanvas.sortingLayerID = sourceCanvas.sortingLayerID;
            sortingCanvas.additionalShaderChannels = sourceCanvas.additionalShaderChannels;
            sortingCanvas.sortingOrder = sourceCanvas.sortingOrder + SortingOrderOffset;
        }
        else
        {
            sortingCanvas.sortingOrder = SortingOrderOffset;
        }

        sortingCanvas.overrideSorting = true;
    }

    private static Canvas FindParentCanvas(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            if (current.TryGetComponent(out Canvas canvas))
                return canvas;

            current = current.parent;
        }

        return null;
    }

    private void DisableRaycastTargets()
    {
        if (generatedClip == null)
            return;

        Graphic[] graphics = generatedClip.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private Rect CalculateAreaInParent(RectTransform parent)
    {
        if (scanArea == null)
            return default;

        if (parent == null)
            return scanArea.rect;

        scanArea.GetWorldCorners(areaCorners);

        Vector3 first = parent.InverseTransformPoint(areaCorners[0]);
        Rect rect = new Rect(first.x, first.y, 0f, 0f);

        for (int i = 1; i < areaCorners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(areaCorners[i]);
            rect.xMin = Mathf.Min(rect.xMin, local.x);
            rect.xMax = Mathf.Max(rect.xMax, local.x);
            rect.yMin = Mathf.Min(rect.yMin, local.y);
            rect.yMax = Mathf.Max(rect.yMax, local.y);
        }

        return rect;
    }

    private void SetClipPosition(float x, float y)
    {
        if (generatedClip == null)
            return;

        generatedClip.localPosition = new Vector3(x, y, generatedClip.localPosition.z);
    }

    private void SetAlpha(float alpha)
    {
        if (generatedOverlayImage == null)
            return;

        Color color = new Color(0f, 1f, 1f, alpha);
        generatedOverlayImage.color = color;
    }

    private static Image FindSourceImage(RectTransform root)
    {
        if (root == null)
            return null;

        Image image = root.GetComponent<Image>();
        if (image != null && image.sprite != null)
            return image;

        Image[] children = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].sprite != null)
                return children[i];
        }

        return null;
    }

    private static bool HasDirectImage(RectTransform target)
    {
        if (target == null)
            return false;

        Image image = target.GetComponent<Image>();
        return image != null && image.sprite != null;
    }

    private static Vector3 DivideLossyScale(Vector3 source, Vector3 parent)
    {
        return new Vector3(
            parent.x == 0f ? source.x : source.x / parent.x,
            parent.y == 0f ? source.y : source.y / parent.y,
            parent.z == 0f ? source.z : source.z / parent.z);
    }
}
