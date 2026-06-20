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
    static readonly Color WarningHighlightColor = new Color(1f, 0.08f, 0.04f, 1f);

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

    [Header("Jam Feedback")]
    [FormerlySerializedAs("failedWindowMoveAmount")]
    [Range(0f, 1f)]
    [SerializeField] float jamWindowMoveAmount = 0.12f;
    [Min(0f)]
    [FormerlySerializedAs("failedWindowHoldDuration")]
    [SerializeField] float jamWindowHoldDuration = 0.08f;
    [Min(0.01f)]
    [SerializeField] float jamWindowMoveDuration = 0.18f;
    [Min(1)]
    [SerializeField] int jamBeltFrameSteps = 3;
    [Min(0.01f)]
    [SerializeField] float jamBeltFrameInterval = 0.07f;
    [Min(1)]
    [SerializeField] int jamWarningFlashCount = 3;
    [Min(0.01f)]
    [SerializeField] float jamWarningFlashInterval = 0.09f;
    [Range(0f, 1f)]
    [SerializeField] float jamWarningOverlayAlpha = 0.38f;

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
    [SerializeField, HideInInspector] string blockingItemId = "Skin_Flake";
    [SerializeField, HideInInspector] bool syncBlockingItemFromChildItem = true;

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
        SyncBlockingItemStateFromChildItem();

        if (windowVisualMode != WindowVisualMode.MoveOnly)
            SetWindowVisualAmount(0f);
    }

    void OnEnable()
    {
        GameEvent.EOnItemCollected += HandleItemCollected;
    }

    void OnDisable()
    {
        GameEvent.EOnItemCollected -= HandleItemCollected;
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

    void HandleItemCollected(ItemData itemData)
    {
        if (itemData == null || itemData.itemID != blockingItemId)
            return;

        ClearBlockingItem();
    }

    void SyncBlockingItemStateFromChildItem()
    {
        if (!syncBlockingItemFromChildItem || string.IsNullOrEmpty(blockingItemId))
            return;

        ToolItem[] childItems = GetComponentsInChildren<ToolItem>(true);
        foreach (ToolItem item in childItems)
        {
            if (item == null || item._itemData == null)
                continue;

            if (item._itemData.itemID == blockingItemId && item.gameObject.activeInHierarchy)
            {
                blockingItemPresent = true;
                return;
            }
        }
    }

    IEnumerator SawPuzzleMissingRoutine()
    {
        SetBusy(true);

        onSawPuzzleMissingFeedback?.Invoke();
        yield return AnimateFrames(driveFeedbackRenderer, driveFeedbackFrames, FailedBeltSteps);
        yield return Shake(driveFeedbackShakeTarget != null
            ? driveFeedbackShakeTarget
            : shakeTarget != null ? shakeTarget : transform);
        yield return Flash(missingDriveWarning, null, WarningHighlightColor, WarningHighlightColor);

        SetBusy(false);
        activeRoutine = null;
    }

    IEnumerator BlockingItemRoutine()
    {
        SetBusy(true);

        onBlockingItemFeedback?.Invoke();
        Sprite originalBeltSprite = beltRenderer != null ? beltRenderer.sprite : null;

        Coroutine windowDownRoutine = StartCoroutine(MoveWindow(
            jamWindowMoveAmount,
            jamWindowMoveDuration));
        Coroutine beltRoutine = StartCoroutine(JamBeltBlinkRoutine(originalBeltSprite));
        Coroutine shakeRoutine = StartCoroutine(Shake(shakeTarget != null ? shakeTarget : transform));
        Coroutine flashRoutine = StartCoroutine(Flash(
            jamWarning,
            jamTargetHighlight,
            JamOverlayColor(),
            WarningHighlightColor,
            jamWarningFlashCount,
            jamWarningFlashInterval));

        yield return windowDownRoutine;
        yield return beltRoutine;
        yield return shakeRoutine;
        yield return flashRoutine;

        if (jamWindowHoldDuration > 0f)
            yield return new WaitForSeconds(jamWindowHoldDuration);

        yield return MoveWindow(0f, jamWindowMoveDuration);

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
        yield return MoveWindow(targetAmount, windowMoveDuration);
    }

    IEnumerator MoveWindow(float targetAmount, float moveDuration)
    {
        if (safetyWindow == null)
            yield break;

        CacheInitialPositions();

        Vector3 start = safetyWindow.localPosition;
        Vector3 target = windowClosedLocalPosition + windowDownLocalOffset * targetAmount;
        float elapsed = 0f;

        float duration = Mathf.Max(0.01f, moveDuration);
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
        yield return AnimateBeltFrames(steps, beltFrameInterval, false);
    }

    IEnumerator AnimateBeltFrames(int steps, float frameInterval, bool restoreOriginal)
    {
        yield return AnimateFrames(beltRenderer, beltFrames, steps, frameInterval, restoreOriginal);
    }

    IEnumerator AnimateFrames(SpriteRenderer targetRenderer, Sprite[] frames, int steps)
    {
        yield return AnimateFrames(targetRenderer, frames, steps, beltFrameInterval, false);
    }

    IEnumerator AnimateFrames(
        SpriteRenderer targetRenderer,
        Sprite[] frames,
        int steps,
        float frameInterval,
        bool restoreOriginal)
    {
        if (targetRenderer == null || frames == null || frames.Length == 0 || steps <= 0)
            yield break;

        Sprite originalSprite = targetRenderer.sprite;

        for (int i = 0; i < steps; i++)
        {
            targetRenderer.sprite = frames[i % frames.Length];
            yield return new WaitForSeconds(Mathf.Max(0.01f, frameInterval));
        }

        if (restoreOriginal)
            targetRenderer.sprite = originalSprite;
    }

    IEnumerator JamBeltBlinkRoutine(Sprite originalSprite)
    {
        if (beltRenderer == null)
            yield break;

        Sprite nextSprite = GetNextBeltFrame(originalSprite);
        if (nextSprite == null)
            yield break;

        int blinkCount = Mathf.Max(1, jamBeltFrameSteps);
        float interval = Mathf.Max(0.01f, jamBeltFrameInterval);

        for (int i = 0; i < blinkCount; i++)
        {
            beltRenderer.sprite = nextSprite;
            yield return new WaitForSeconds(interval);

            RestoreBeltSprite(originalSprite);

            if (i < blinkCount - 1)
                yield return new WaitForSeconds(interval);
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

    IEnumerator Flash(
        SpriteRenderer primary,
        SpriteRenderer secondary,
        Color primaryFlashColor,
        Color secondaryFlashColor)
    {
        yield return Flash(
            primary,
            secondary,
            primaryFlashColor,
            secondaryFlashColor,
            WarningFlashCount,
            WarningFlashInterval);
    }

    IEnumerator Flash(
        SpriteRenderer primary,
        SpriteRenderer secondary,
        Color primaryFlashColor,
        Color secondaryFlashColor,
        int flashCount,
        float flashInterval)
    {
        if (primary == null && secondary == null)
            yield break;

        RendererState primaryState = Capture(primary);
        RendererState secondaryState = Capture(secondary);

        int count = Mathf.Max(1, flashCount);
        float interval = Mathf.Max(0.01f, flashInterval);

        for (int i = 0; i < count; i++)
        {
            ApplyFlash(primary, primaryFlashColor, true);
            ApplyFlash(secondary, secondaryFlashColor, true);
            yield return new WaitForSeconds(interval);

            ApplyFlash(primary, primaryFlashColor, false);
            ApplyFlash(secondary, secondaryFlashColor, false);
            yield return new WaitForSeconds(interval);
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

    Color JamOverlayColor()
    {
        return new Color(1f, 0.04f, 0.02f, Mathf.Clamp01(jamWarningOverlayAlpha));
    }

    void RestoreBeltSprite(Sprite sprite)
    {
        if (beltRenderer != null)
            beltRenderer.sprite = sprite;
    }

    Sprite GetNextBeltFrame(Sprite currentSprite)
    {
        if (beltFrames == null || beltFrames.Length == 0)
            return null;

        if (beltFrames.Length == 1)
            return beltFrames[0];

        for (int i = 0; i < beltFrames.Length; i++)
        {
            if (beltFrames[i] == currentSprite)
                return beltFrames[(i + 1) % beltFrames.Length];
        }

        return beltFrames[1] != null ? beltFrames[1] : beltFrames[0];
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
