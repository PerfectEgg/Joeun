using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Marks a connected multi-piece assembly as ready, then switches to the
/// completed visual when the Assemble skill is used on it.
/// </summary>
public class ShowCompletedAssembly : MonoBehaviour
{
    private static readonly List<ShowCompletedAssembly> ActiveAssemblies = new List<ShowCompletedAssembly>();
    private const float ReadyActivationDelay = 0.16f;

    [Header("Puzzle Events")]
    [SerializeField] private ConnectPuzzleManager puzzleManager;
    [SerializeField] private UnityEvent onAssembleReady;
    [SerializeField] private UnityEvent onAssembled;

    [Header("Assemble Gate")]
    [SerializeField] private RectTransform clickArea;

    [Header("Hide Visuals")]
    [SerializeField] private Image[] hideImages;

    [Header("Disable Components")]
    [SerializeField] private Behaviour[] disableBehaviours;

    [Header("Hide Objects")]
    [SerializeField] private GameObject[] hideObjects;

    [Header("Show Objects")]
    [SerializeField] private GameObject[] showObjects;

    private bool autoBindPuzzleManagerEvents = true;
    private bool waitForAssembleSkill = true;
    private bool showOnAssembleClick = true;
    private bool showImmediatelyIfAlreadyInAssembleMode = false;
    private float switchDelay = 0f;
    private float showFadeDuration = 0f;

    private Coroutine readyRoutine;
    private Coroutine showRoutine;
    private bool isReadyForAssemble;
    private bool isCompletedVisualShown;
    private bool managerEventsBound;
    private bool inputQuietAfterReady;
    private bool assembledEventInvoked;
    private int readyFrame;
    private AssembleCompositeOutlineTarget outlineTarget;
    private AssemblyScanLineEffect scanLineEffect;
    private readonly List<CompletedAssemblyAnchorAligner> completedVisualAligners =
        new List<CompletedAssemblyAnchorAligner>();

    public bool IsReadyForAssemble => isReadyForAssemble && !isCompletedVisualShown;
    public bool IsCompletedVisualShown => isCompletedVisualShown;

    private void Awake()
    {
        ResolvePuzzleManager();
        ResolveOutlineTarget();
        ResolveScanLineEffect();
    }

    private void OnEnable()
    {
        if (!ActiveAssemblies.Contains(this))
            ActiveAssemblies.Add(this);

        ResolveOutlineTarget();
        ResolveScanLineEffect();
        BindPuzzleManagerEvents();

        if (isCompletedVisualShown)
            ApplyCompletedVisualImmediate(!assembledEventInvoked);
        else if (puzzleManager != null && puzzleManager.IsSolved)
            MarkReady();
    }

    private void OnDisable()
    {
        StopReadyRoutine();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (scanLineEffect != null)
            scanLineEffect.Hide();

        ActiveAssemblies.Remove(this);
        UnbindPuzzleManagerEvents();
    }

    private void Update()
    {
        UpdateAssemblyClickArming();

        if (!showOnAssembleClick
            || !CanAcceptAssemblyInput()
            || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (waitForAssembleSkill && !IsAssembleMode())
            return;

        if (!IsPointerInsideAssemblyVisual(Input.mousePosition))
            return;

        TryShowFromAssemble();
    }

    /// <summary>
    /// Kept as the UnityEvent entry point. This no longer swaps sprites
    /// immediately; it only marks the assembly as ready for Assemble.
    /// </summary>
    public void Show()
    {
        MarkReady();

        if (!waitForAssembleSkill || showImmediatelyIfAlreadyInAssembleMode && IsAssembleMode())
            TryShowFromAssemble();
    }

    public void MarkReady()
    {
        if (isCompletedVisualShown || isReadyForAssemble || readyRoutine != null)
            return;

        readyRoutine = StartCoroutine(ActivateReadyAfterSnapSettles());
    }

    private IEnumerator ActivateReadyAfterSnapSettles()
    {
        if (ReadyActivationDelay > 0f)
            yield return new WaitForSeconds(ReadyActivationDelay);

        readyRoutine = null;
        if (isCompletedVisualShown)
            yield break;

        ResolveOutlineTarget();
        if (outlineTarget != null)
            outlineTarget.PrepareOutline();

        isReadyForAssemble = true;
        inputQuietAfterReady = false;
        readyFrame = Time.frameCount;
        onAssembleReady?.Invoke();
    }

    public void TryShowFromAssemble()
    {
        if (!CanAcceptAssemblyInput())
            return;

        if (waitForAssembleSkill && !IsAssembleMode())
            return;

        ShowCompletedVisual();
    }

    public void ShowCompletedVisual()
    {
        StopReadyRoutine();

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        isReadyForAssemble = false;
        isCompletedVisualShown = true;
        assembledEventInvoked = false;
        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void ShowImmediate()
    {
        StopReadyRoutine();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        ResolveScanLineEffect();
        if (scanLineEffect != null)
            scanLineEffect.Hide();

        isReadyForAssemble = false;
        isCompletedVisualShown = true;
        assembledEventInvoked = false;
        ApplyCompletedVisualImmediate(true);
    }

    public void ResetState()
    {
        StopReadyRoutine();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        ResolveScanLineEffect();
        if (scanLineEffect != null)
            scanLineEffect.Hide();

        isReadyForAssemble = false;
        isCompletedVisualShown = false;
        assembledEventInvoked = false;
        SetImagesEnabled(hideImages, true);
        SetBehavioursEnabled(disableBehaviours, true);
        SetObjectsActive(hideObjects, true);
        SetObjectsActive(showObjects, false);
        SetShowObjectsAlpha(1f);
    }

    private void ApplyCompletedVisualImmediate(bool invokeEvent)
    {
        ResolveScanLineEffect();
        if (scanLineEffect != null)
            scanLineEffect.Hide();

        PrepareCompletedVisualObjects();
        ApplyCompletedVisualAlignment();
        SetImagesEnabled(hideImages, false);
        SetBehavioursEnabled(disableBehaviours, false);
        SetObjectsActive(hideObjects, false);
        SetObjectsActive(showObjects, true);
        SetShowObjectsAlpha(1f);

        if (invokeEvent)
            InvokeAssembledOnce();
    }

    private void InvokeAssembledOnce()
    {
        if (assembledEventInvoked)
            return;

        assembledEventInvoked = true;
        onAssembled?.Invoke();
    }

    private void StopReadyRoutine()
    {
        if (readyRoutine == null)
            return;

        StopCoroutine(readyRoutine);
        readyRoutine = null;
    }

    private IEnumerator ShowRoutine()
    {
        if (switchDelay > 0f)
            yield return new WaitForSeconds(switchDelay);

        PrepareCompletedVisualObjects();
        ApplyCompletedVisualAlignment();
        SetShowObjectsAlpha(showFadeDuration > 0f ? 0f : 1f);
        SetObjectsActive(showObjects, true);

        if (showFadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < showFadeDuration)
            {
                elapsed += Time.deltaTime;
                SetShowObjectsAlpha(Mathf.Clamp01(elapsed / showFadeDuration));
                yield return null;
            }
        }

        SetShowObjectsAlpha(1f);
        SetImagesEnabled(hideImages, false);
        SetBehavioursEnabled(disableBehaviours, false);
        SetObjectsActive(hideObjects, false);

        ResolveScanLineEffect();
        if (scanLineEffect != null)
            yield return StartCoroutine(scanLineEffect.Play());

        showRoutine = null;
        InvokeAssembledOnce();
    }

    private void ResolvePuzzleManager()
    {
        if (puzzleManager != null)
            return;

        puzzleManager = GetComponent<ConnectPuzzleManager>();
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<ConnectPuzzleManager>();
        if (puzzleManager == null)
            puzzleManager = GetComponentInChildren<ConnectPuzzleManager>(true);
    }

    private void ResolveOutlineTarget()
    {
        if (outlineTarget != null)
            return;

        outlineTarget = GetComponent<AssembleCompositeOutlineTarget>();
        if (outlineTarget == null)
            outlineTarget = GetComponentInParent<AssembleCompositeOutlineTarget>();
        if (outlineTarget == null)
            outlineTarget = GetComponentInChildren<AssembleCompositeOutlineTarget>(true);
        if (outlineTarget == null && puzzleManager != null)
            outlineTarget = puzzleManager.GetComponentInParent<AssembleCompositeOutlineTarget>();
    }

    private void ResolveScanLineEffect()
    {
        if (scanLineEffect != null)
            return;

        scanLineEffect = GetComponent<AssemblyScanLineEffect>();
        if (scanLineEffect == null)
            scanLineEffect = GetComponentInParent<AssemblyScanLineEffect>();
        if (scanLineEffect == null)
            scanLineEffect = GetComponentInChildren<AssemblyScanLineEffect>(true);
        if (scanLineEffect == null && puzzleManager != null)
            scanLineEffect = puzzleManager.GetComponentInParent<AssemblyScanLineEffect>();
        if (scanLineEffect == null && transform is RectTransform)
            scanLineEffect = gameObject.AddComponent<AssemblyScanLineEffect>();

        if (scanLineEffect != null)
            scanLineEffect.SetScanArea(FindCompletedVisualScanArea());
    }

    private RectTransform FindCompletedVisualScanArea()
    {
        if (showObjects == null)
            return null;

        foreach (GameObject obj in showObjects)
        {
            if (obj == null)
                continue;

            Image image = obj.GetComponent<Image>();
            if (image != null && image.sprite != null)
                return image.rectTransform;

            Image[] childImages = obj.GetComponentsInChildren<Image>(true);
            foreach (Image childImage in childImages)
            {
                if (childImage != null && childImage.sprite != null)
                    return childImage.rectTransform;
            }
        }

        return null;
    }

    private void BindPuzzleManagerEvents()
    {
        if (!autoBindPuzzleManagerEvents || managerEventsBound)
            return;

        ResolvePuzzleManager();
        if (puzzleManager == null)
            return;

        puzzleManager.onPuzzleSolved.AddListener(Show);
        puzzleManager.onPuzzleReset.AddListener(ResetState);
        managerEventsBound = true;
    }

    private void UnbindPuzzleManagerEvents()
    {
        if (!managerEventsBound || puzzleManager == null)
            return;

        puzzleManager.onPuzzleSolved.RemoveListener(Show);
        puzzleManager.onPuzzleReset.RemoveListener(ResetState);
        managerEventsBound = false;
    }

    private static bool IsAssembleMode()
    {
        if (SkillInteractionLock.IsLocked)
            return false;

        return SkillIconModeView.CurrentMode == SkillModeType.Assemble
            || PuzzleModeManager.Instance != null && PuzzleModeManager.Instance.IsAssemble;
    }

    private void UpdateAssemblyClickArming()
    {
        if (!IsReadyForAssemble || inputQuietAfterReady || Time.frameCount <= readyFrame)
            return;

        if (!IsPrimaryMouseActiveThisFrame())
            inputQuietAfterReady = true;
    }

    private bool CanAcceptAssemblyInput()
    {
        UpdateAssemblyClickArming();
        return IsReadyForAssemble && inputQuietAfterReady;
    }

    private static bool IsPrimaryMouseActiveThisFrame()
    {
        return Input.GetMouseButton(0)
            || Input.GetMouseButtonDown(0)
            || Input.GetMouseButtonUp(0);
    }

    public static bool IsReadyAssemblyClick(Vector2 screenPoint)
    {
        for (int i = ActiveAssemblies.Count - 1; i >= 0; i--)
        {
            ShowCompletedAssembly assembly = ActiveAssemblies[i];
            if (assembly == null)
            {
                ActiveAssemblies.RemoveAt(i);
                continue;
            }

            if (assembly.CanAssembleFromPointer(screenPoint))
                return true;
        }

        return false;
    }

    private bool CanAssembleFromPointer(Vector2 screenPoint)
    {
        if (!showOnAssembleClick || !CanAcceptAssemblyInput())
            return false;

        if (waitForAssembleSkill && !IsAssembleMode())
            return false;

        return IsPointerInsideAssemblyVisual(screenPoint);
    }

    private bool IsPointerInsideAssemblyVisual(Vector2 screenPoint)
    {
        ResolveOutlineTarget();
        if (outlineTarget != null)
            return outlineTarget.ContainsScreenPoint(screenPoint);

        if (clickArea != null && ContainsScreenPoint(clickArea, screenPoint))
            return true;

        if (ContainsAnyImage(hideImages, screenPoint)
            || ContainsAnyObject(hideObjects, screenPoint)
            || ContainsAnyObject(showObjects, screenPoint))
        {
            return true;
        }

        RectTransform rect = transform as RectTransform;
        return rect != null && ContainsScreenPoint(rect, screenPoint);
    }

    private static bool ContainsAnyImage(Image[] images, Vector2 screenPoint)
    {
        if (images == null)
            return false;

        foreach (Image image in images)
        {
            if (image == null || !image.gameObject.activeInHierarchy)
                continue;

            if (ContainsScreenPoint(image.rectTransform, screenPoint))
                return true;
        }

        return false;
    }

    private static bool ContainsAnyObject(GameObject[] objects, Vector2 screenPoint)
    {
        if (objects == null)
            return false;

        foreach (GameObject obj in objects)
        {
            if (obj == null || !obj.activeInHierarchy)
                continue;

            RectTransform rect = obj.transform as RectTransform;
            if (rect != null && ContainsScreenPoint(rect, screenPoint))
                return true;
        }

        return false;
    }

    private static bool ContainsScreenPoint(RectTransform rect, Vector2 screenPoint)
    {
        if (rect == null)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
    }

    private static void SetImagesEnabled(Image[] images, bool enabled)
    {
        if (images == null)
            return;

        foreach (Image image in images)
        {
            if (image != null)
                image.enabled = enabled;
        }
    }

    private static void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void ApplyCompletedVisualAlignment()
    {
        completedVisualAligners.Clear();
        AddCompletedVisualAligners(gameObject);

        if (showObjects != null)
        {
            foreach (GameObject obj in showObjects)
                AddCompletedVisualAligners(obj);
        }

        foreach (CompletedAssemblyAnchorAligner aligner in completedVisualAligners)
        {
            if (aligner != null)
                aligner.ApplyAlignment();
        }
    }

    private void PrepareCompletedVisualObjects()
    {
        if (showObjects == null)
            return;

        foreach (GameObject obj in showObjects)
            PrepareCompletedVisualObject(obj);
    }

    private static void PrepareCompletedVisualObject(GameObject obj)
    {
        if (obj == null)
            return;

        DisableComponents(obj.GetComponentsInChildren<PuzzlePart>(true));
        DisableComponents(obj.GetComponentsInChildren<PuzzlePartRenderOrderController>(true));

        if (obj.GetComponentInChildren<CompletedAssemblyDragHandle>(true) == null
            && obj.transform is RectTransform)
        {
            obj.AddComponent<CompletedAssemblyDragHandle>();
        }
    }

    private static void DisableComponents<T>(T[] components) where T : Behaviour
    {
        if (components == null)
            return;

        foreach (T component in components)
        {
            if (component != null)
                component.enabled = false;
        }
    }

    private void AddCompletedVisualAligners(GameObject root)
    {
        if (root == null)
            return;

        CompletedAssemblyAnchorAligner[] aligners =
            root.GetComponentsInChildren<CompletedAssemblyAnchorAligner>(true);
        foreach (CompletedAssemblyAnchorAligner aligner in aligners)
        {
            if (aligner != null && !completedVisualAligners.Contains(aligner))
                completedVisualAligners.Add(aligner);
        }
    }

    private void SetShowObjectsAlpha(float alpha)
    {
        if (showObjects == null)
            return;

        foreach (GameObject obj in showObjects)
        {
            if (obj == null)
                continue;

            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group == null)
                group = obj.AddComponent<CanvasGroup>();

            group.alpha = alpha;
        }
    }
}
