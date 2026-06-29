using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CoolantPipeFillTintEffect : MonoBehaviour
{
    [Header("Pipe Images")]
    [SerializeField] private List<Image> stage1Pipes = new List<Image>();
    [SerializeField] private List<Image> stage2Pipes = new List<Image>();

    [Header("Color")]
    [SerializeField] private Color baseColor = new Color32(150, 150, 150, 255);
    [SerializeField] private Color flowColor = Color.white;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float duration = 0.55f;
    [SerializeField] private AnimationCurve fillCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<Image, Image> overlays = new Dictionary<Image, Image>();
    private Coroutine stage1Routine;
    private Coroutine stage2Routine;

    private void Awake()
    {
        ApplyBaseColor(stage1Pipes);
        ApplyBaseColor(stage2Pipes);
        HideOverlays();
    }

    public void PlayStage1Flow()
    {
        stage1Routine = Restart(stage1Routine, stage1Pipes);
    }

    public void PlayStage2Flow()
    {
        stage2Routine = Restart(stage2Routine, stage2Pipes);
    }

    public void ResetFlow()
    {
        StopRoutine(stage1Routine);
        StopRoutine(stage2Routine);
        stage1Routine = null;
        stage2Routine = null;

        ApplyBaseColor(stage1Pipes);
        ApplyBaseColor(stage2Pipes);
        HideOverlays();
    }

    private Coroutine Restart(Coroutine routine, List<Image> pipes)
    {
        StopRoutine(routine);
        return StartCoroutine(PlayRoutine(pipes));
    }

    private void StopRoutine(Coroutine routine)
    {
        if (routine != null)
            StopCoroutine(routine);
    }

    private IEnumerator PlayRoutine(List<Image> pipes)
    {
        List<Image> validPipes = GetValidPipes(pipes);
        if (validPipes.Count == 0)
            yield break;

        foreach (Image pipe in validPipes)
        {
            PrepareSourceImage(pipe);
            Image overlay = GetOrCreateOverlay(pipe);
            PrepareOverlay(pipe, overlay);
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float amount = fillCurve.Evaluate(Mathf.Clamp01(t / duration));

            foreach (Image pipe in validPipes)
                overlays[pipe].fillAmount = amount;

            yield return null;
        }

        foreach (Image pipe in validPipes)
        {
            pipe.color = flowColor;
            HideOverlay(overlays[pipe]);
        }
    }

    private Image GetOrCreateOverlay(Image source)
    {
        if (overlays.TryGetValue(source, out Image overlay) && overlay != null)
            return overlay;

        GameObject obj = new GameObject($"{source.gameObject.name}_FlowOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(source.transform.parent, false);
        obj.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        RectTransform sourceRect = source.rectTransform;
        RectTransform overlayRect = obj.GetComponent<RectTransform>();
        overlayRect.anchorMin = sourceRect.anchorMin;
        overlayRect.anchorMax = sourceRect.anchorMax;
        overlayRect.anchoredPosition = sourceRect.anchoredPosition;
        overlayRect.sizeDelta = sourceRect.sizeDelta;
        overlayRect.pivot = sourceRect.pivot;
        overlayRect.localRotation = sourceRect.localRotation;
        overlayRect.localScale = sourceRect.localScale;

        overlay = obj.GetComponent<Image>();
        overlay.raycastTarget = false;
        overlays[source] = overlay;
        return overlay;
    }

    private void PrepareOverlay(Image source, Image overlay)
    {
        overlay.gameObject.SetActive(true);
        overlay.sprite = source.sprite;
        overlay.color = flowColor;
        overlay.material = source.material;
        overlay.preserveAspect = source.preserveAspect;
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Vertical;
        overlay.fillOrigin = (int)Image.OriginVertical.Top;
        overlay.fillAmount = 0f;

        RectTransform sourceRect = source.rectTransform;
        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchoredPosition = sourceRect.anchoredPosition;
        overlayRect.sizeDelta = sourceRect.sizeDelta;
        overlayRect.localRotation = sourceRect.localRotation;
        overlayRect.localScale = sourceRect.localScale;
    }

    private static List<Image> GetValidPipes(List<Image> pipes)
    {
        List<Image> validPipes = new List<Image>();
        if (pipes == null)
            return validPipes;

        foreach (Image pipe in pipes)
        {
            if (pipe != null)
                validPipes.Add(pipe);
        }

        return validPipes;
    }

    private void ApplyBaseColor(List<Image> pipes)
    {
        if (pipes == null)
            return;

        foreach (Image pipe in pipes)
        {
            if (pipe != null)
                PrepareSourceImage(pipe);
        }
    }

    private void PrepareSourceImage(Image pipe)
    {
        pipe.color = baseColor;
        pipe.raycastTarget = false;
    }

    private void HideOverlays()
    {
        foreach (Image overlay in overlays.Values)
            HideOverlay(overlay);
    }

    private static void HideOverlay(Image overlay)
    {
        if (overlay == null)
            return;

        overlay.fillAmount = 0f;
        overlay.gameObject.SetActive(false);
    }
}
