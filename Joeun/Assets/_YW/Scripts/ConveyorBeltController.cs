using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Controls the conveyor worktable sequence:
/// missing drive feedback, jam feedback, and the normal window + belt run.
/// </summary>
[DisallowMultipleComponent]
public class ConveyorBeltController : MonoBehaviour, IInteractive
{
    enum WindowVisualMode
    {
        MoveOnly,
        FillImage,
        ScaleY
    }

    const int FailedBeltSteps = 3;
    const float FailShakeDuration = 0.16f;
    const float FailShakeStrength = 0.035f;
    const int WarningFlashCount = 3;
    const float WarningFlashInterval = 0.09f;

    [Header("State")]
    [FormerlySerializedAs("driveReady")]
    [SerializeField] bool sawPuzzleSolved;
    [FormerlySerializedAs("jammed")]
    [SerializeField] bool blockingItemPresent = true;

    [Header("Belt Motion")]
    [SerializeField] SpriteRenderer beltRenderer;
    [SerializeField] Sprite[] beltFrames;
    [Min(1)]
    [SerializeField] int beltRunCycles = 4;
    [Min(0.01f)]
    [SerializeField] float beltFrameInterval = 0.07f;

    [Header("Missing Saw Puzzle Feedback")]
    [SerializeField] SpriteRenderer driveFeedbackRenderer;
    [SerializeField] Sprite[] driveFeedbackFrames;
    [SerializeField] Transform driveFeedbackShakeTarget;

    [Header("Safety Window")]
    [SerializeField] Transform safetyWindow;
    [SerializeField] Vector3 windowDownLocalOffset = new Vector3(0f, -1f, 0f);
    [Min(0.01f)]
    [SerializeField] float windowMoveDuration = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] float failedWindowMoveAmount = 0.18f;
    [Min(0f)]
    [SerializeField] float failedWindowHoldDuration = 0.12f;

    [Header("Feedback")]
    [SerializeField] Transform shakeTarget;
    [SerializeField] SpriteRenderer missingDriveWarning;
    [SerializeField] SpriteRenderer jamWarning;
    [SerializeField] SpriteRenderer jamTargetHighlight;

    [SerializeField, HideInInspector] WindowVisualMode windowVisualMode = WindowVisualMode.MoveOnly;
    [SerializeField, HideInInspector] Transform safetyWindowScaleTarget;
    [SerializeField, HideInInspector, Range(0f, 1f)] float windowClosedScaleY = 0f;
    [SerializeField, HideInInspector, Range(0f, 1f)] float windowDownScaleY = 1f;
    [SerializeField, HideInInspector] Image safetyWindowFillImage;
    [SerializeField, HideInInspector, Range(0f, 1f)] float windowClosedFillAmount = 0f;
    [SerializeField, HideInInspector, Range(0f, 1f)] float windowDownFillAmount = 1f;

    [SerializeField, HideInInspector] Collider2D[] disableCollidersWhileBusy;
    [SerializeField, HideInInspector] Behaviour[] disableBehavioursWhileBusy;

    [FormerlySerializedAs("onDriveReady")]
    [SerializeField, HideInInspector] UnityEvent onSawPuzzleSolved;
    [FormerlySerializedAs("onJamCleared")]
    [SerializeField, HideInInspector] UnityEvent onBlockingItemCleared;
    [FormerlySerializedAs("onMissingDriveFeedback")]
    [SerializeField, HideInInspector] UnityEvent onSawPuzzleMissingFeedback;
    [FormerlySerializedAs("onJamFeedback")]
    [SerializeField, HideInInspector] UnityEvent onBlockingItemFeedback;
    [SerializeField, HideInInspector] UnityEvent onRunStarted;
    [SerializeField, HideInInspector] UnityEvent onRunFinished;

    Coroutine activeRoutine;
    Vector3 windowClosedLocalPosition;
    Vector3 windowBaseLocalScale;
    bool hasWindowClosedPosition;
    bool hasWindowBaseScale;

    public bool SawPuzzleSolved => sawPuzzleSolved;
    public bool BlockingItemPresent => blockingItemPresent;
    public bool DriveReady => sawPuzzleSolved;
    public bool Jammed => blockingItemPresent;
    public bool IsBusy => activeRoutine != null;

    void Reset()
    {
        beltRenderer = GetComponentInChildren<SpriteRenderer>();
        shakeTarget = transform;
    }

    void Awake()
    {
        CacheInitialPositions();
        CacheWindowScale();

        if (windowVisualMode != WindowVisualMode.MoveOnly)
            SetWindowVisualAmount(0f);
    }

    public void Interact()
    {
        PressButton();
    }

    public void PressButton()
    {
        if (activeRoutine != null)
            return;

        if (!sawPuzzleSolved)
        {
            activeRoutine = StartCoroutine(SawPuzzleMissingRoutine());
            return;
        }

        if (blockingItemPresent)
        {
            activeRoutine = StartCoroutine(BlockingItemRoutine());
            return;
        }

        activeRoutine = StartCoroutine(RunRoutine());
    }

    public void MarkSawPuzzleSolved()
    {
        if (sawPuzzleSolved)
            return;

        sawPuzzleSolved = true;
        onSawPuzzleSolved?.Invoke();
    }

    public void ClearBlockingItem()
    {
        if (!blockingItemPresent)
            return;

        blockingItemPresent = false;
        onBlockingItemCleared?.Invoke();
    }

    public void SetSawPuzzleSolved(bool solved)
    {
        sawPuzzleSolved = solved;
        if (sawPuzzleSolved)
            onSawPuzzleSolved?.Invoke();
    }

    public void SetBlockingItemPresent(bool present)
    {
        blockingItemPresent = present;
        if (!blockingItemPresent)
            onBlockingItemCleared?.Invoke();
    }

    public void MarkDriveReady()
    {
        MarkSawPuzzleSolved();
    }

    public void ClearJam()
    {
        ClearBlockingItem();
    }

    public void SetDriveReady(bool ready)
    {
        SetSawPuzzleSolved(ready);
    }

    public void SetJammed(bool isJammed)
    {
        SetBlockingItemPresent(isJammed);
    }

    IEnumerator SawPuzzleMissingRoutine()
    {
        SetBusy(true);

        onSawPuzzleMissingFeedback?.Invoke();
        yield return AnimateFrames(driveFeedbackRenderer, driveFeedbackFrames, FailedBeltSteps);
        yield return Shake(driveFeedbackShakeTarget != null
            ? driveFeedbackShakeTarget
            : shakeTarget != null ? shakeTarget : transform);
        yield return Flash(missingDriveWarning, null);

        SetBusy(false);
        activeRoutine = null;
    }

    IEnumerator BlockingItemRoutine()
    {
        SetBusy(true);

        yield return MoveWindow(failedWindowMoveAmount);
        onBlockingItemFeedback?.Invoke();
        yield return AnimateBeltFrames(FailedBeltSteps);
        yield return Shake(shakeTarget != null ? shakeTarget : transform);
        yield return Flash(jamWarning, jamTargetHighlight);
        yield return new WaitForSeconds(Mathf.Max(0f, failedWindowHoldDuration));
        yield return MoveWindow(0f);

        SetBusy(false);
        activeRoutine = null;
    }

    IEnumerator RunRoutine()
    {
        SetBusy(true);
        onRunStarted?.Invoke();

        yield return MoveWindow(1f);
        yield return AnimateBeltFrames(beltRunCycles * FrameCount);
        yield return MoveWindow(0f);

        onRunFinished?.Invoke();
        SetBusy(false);
        activeRoutine = null;
    }

    IEnumerator MoveWindow(float targetAmount)
    {
        if (safetyWindow == null)
            yield break;

        CacheInitialPositions();

        Vector3 start = safetyWindow.localPosition;
        Vector3 target = windowClosedLocalPosition + windowDownLocalOffset * targetAmount;
        float elapsed = 0f;

        float duration = Mathf.Max(0.01f, windowMoveDuration);
        float startFillAmount = safetyWindowFillImage != null
            ? safetyWindowFillImage.fillAmount
            : 0f;
        float targetFillAmount = Mathf.Lerp(windowClosedFillAmount, windowDownFillAmount, targetAmount);
        float startScaleAmount = CurrentWindowScaleAmount();
        float targetScaleAmount = Mathf.Lerp(windowClosedScaleY, windowDownScaleY, targetAmount);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            safetyWindow.localPosition = Vector3.Lerp(start, target, t);
            SetWindowFillAmount(Mathf.Lerp(startFillAmount, targetFillAmount, t));
            SetWindowScaleAmount(Mathf.Lerp(startScaleAmount, targetScaleAmount, t));
            yield return null;
        }

        safetyWindow.localPosition = target;
        SetWindowFillAmount(targetFillAmount);
        SetWindowScaleAmount(targetScaleAmount);
    }

    IEnumerator AnimateBeltFrames(int steps)
    {
        yield return AnimateFrames(beltRenderer, beltFrames, steps);
    }

    IEnumerator AnimateFrames(SpriteRenderer targetRenderer, Sprite[] frames, int steps)
    {
        if (targetRenderer == null || frames == null || frames.Length == 0 || steps <= 0)
            yield break;

        for (int i = 0; i < steps; i++)
        {
            targetRenderer.sprite = frames[i % frames.Length];
            yield return new WaitForSeconds(Mathf.Max(0.01f, beltFrameInterval));
        }
    }

    IEnumerator Shake(Transform target)
    {
        if (target == null)
            yield break;

        Vector3 origin = target.localPosition;
        float elapsed = 0f;

        while (elapsed < FailShakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = FailShakeStrength * (1f - elapsed / FailShakeDuration);
            Vector2 offset = Random.insideUnitCircle * strength;
            target.localPosition = origin + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        target.localPosition = origin;
    }

    IEnumerator Flash(SpriteRenderer primary, SpriteRenderer secondary)
    {
        if (primary == null && secondary == null)
            yield break;

        RendererState primaryState = Capture(primary);
        RendererState secondaryState = Capture(secondary);
        Color flashColor = new Color(1f, 0.08f, 0.04f, 1f);

        for (int i = 0; i < WarningFlashCount; i++)
        {
            ApplyFlash(primary, flashColor, true);
            ApplyFlash(secondary, flashColor, true);
            yield return new WaitForSeconds(WarningFlashInterval);

            ApplyFlash(primary, flashColor, false);
            ApplyFlash(secondary, flashColor, false);
            yield return new WaitForSeconds(WarningFlashInterval);
        }

        Restore(primary, primaryState);
        Restore(secondary, secondaryState);
    }

    void CacheInitialPositions()
    {
        if (hasWindowClosedPosition || safetyWindow == null)
            return;

        windowClosedLocalPosition = safetyWindow.localPosition;
        hasWindowClosedPosition = true;
    }

    void CacheWindowScale()
    {
        if (hasWindowBaseScale || safetyWindowScaleTarget == null)
            return;

        windowBaseLocalScale = safetyWindowScaleTarget.localScale;
        hasWindowBaseScale = true;
    }

    void SetWindowVisualAmount(float amount)
    {
        if (windowVisualMode == WindowVisualMode.MoveOnly)
            return;

        float fillAmount = Mathf.Lerp(windowClosedFillAmount, windowDownFillAmount, amount);
        float scaleAmount = Mathf.Lerp(windowClosedScaleY, windowDownScaleY, amount);
        SetWindowFillAmount(fillAmount);
        SetWindowScaleAmount(scaleAmount);
    }

    void SetWindowFillAmount(float amount)
    {
        if (windowVisualMode != WindowVisualMode.FillImage)
            return;

        if (safetyWindowFillImage != null)
            safetyWindowFillImage.fillAmount = Mathf.Clamp01(amount);
    }

    void SetWindowScaleAmount(float amount)
    {
        if (windowVisualMode != WindowVisualMode.ScaleY)
            return;

        if (safetyWindowScaleTarget == null)
            return;

        CacheWindowScale();
        Vector3 scale = windowBaseLocalScale;
        scale.y *= Mathf.Clamp01(amount);
        safetyWindowScaleTarget.localScale = scale;
    }

    float CurrentWindowScaleAmount()
    {
        if (windowVisualMode != WindowVisualMode.ScaleY)
            return windowClosedScaleY;

        if (safetyWindowScaleTarget == null)
            return windowClosedScaleY;

        CacheWindowScale();
        if (Mathf.Abs(windowBaseLocalScale.y) <= 0.0001f)
            return windowClosedScaleY;

        return Mathf.Clamp01(safetyWindowScaleTarget.localScale.y / windowBaseLocalScale.y);
    }

    void SetBusy(bool busy)
    {
        SetCollidersEnabled(disableCollidersWhileBusy, !busy);
        SetBehavioursEnabled(disableBehavioursWhileBusy, !busy);
    }

    int FrameCount => beltFrames != null && beltFrames.Length > 0 ? beltFrames.Length : 1;

    static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    static void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null)
            return;

        foreach (Collider2D collider in colliders)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    static void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    static RendererState Capture(SpriteRenderer renderer)
    {
        if (renderer == null)
            return default;

        return new RendererState(renderer.gameObject.activeSelf, renderer.enabled, renderer.color);
    }

    static void ApplyFlash(SpriteRenderer renderer, Color color, bool on)
    {
        if (renderer == null)
            return;

        renderer.gameObject.SetActive(true);
        renderer.enabled = on;
        if (on)
            renderer.color = color;
    }

    static void Restore(SpriteRenderer renderer, RendererState state)
    {
        if (renderer == null)
            return;

        renderer.color = state.color;
        renderer.enabled = state.enabled;
        renderer.gameObject.SetActive(state.activeSelf);
    }

    struct RendererState
    {
        public readonly bool activeSelf;
        public readonly bool enabled;
        public readonly Color color;

        public RendererState(bool activeSelf, bool enabled, Color color)
        {
            this.activeSelf = activeSelf;
            this.enabled = enabled;
            this.color = color;
        }
    }
}
