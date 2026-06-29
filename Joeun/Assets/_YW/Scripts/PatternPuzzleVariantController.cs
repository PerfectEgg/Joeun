using System.Collections.Generic;
using UnityEngine;

public enum PatternPuzzleVariant
{
    Basic,
    Rotate,
    Assemble,
    Decode
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class PatternPuzzleVariantController : MonoBehaviour
{
    [Header("Variant")]
    [SerializeField] private PatternPuzzleVariant variant = PatternPuzzleVariant.Decode;
    [SerializeField] private bool selectSkillOnOpen = true;
    [SerializeField] private bool clearModeOnClose = true;
    [SerializeField, Min(0f)] private float openHighlightDelay = 0.28f;

    [Header("References")]
    [SerializeField] private PathPuzzleManager pathPuzzle;
    [SerializeField] private PuzzleModeLock modeLock;
    [SerializeField] private PatternPuzzleSkillOnOpen legacySkillOnOpen;
    [SerializeField] private RecognitionDecodePathPuzzleConnector decodeConnector;
    [SerializeField] private GameObject[] decodeOnlyObjects;

    [Header("Required Count")]
    [SerializeField] private bool applyBasicRequiredCount = true;
    [SerializeField, Min(1)] private int basicRequiredCount = 7;

    [Header("Auto Wiring")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool autoCollectDecodeObjects = true;

    private SkillModeType openedSkillMode = SkillModeType.None;
    private bool openedWithSkillMode;
    private SkillModeType pendingOpenSkillMode = SkillModeType.None;
    private int pendingOpenSkillFrames;
    private bool playedOpenSkillSfx;
    private bool delayedOpenHighlight;
    private const int OpenSkillApplyFrames = 60;
    private const string OpenSkillSfxId = "Skill_Select";

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
        ApplyVariant();
    }

    private void OnEnable()
    {
        AutoWire();
        ApplyVariant();
        ApplyOpenSkill();
    }

    private void OnDisable()
    {
        pendingOpenSkillMode = SkillModeType.None;

        if (clearModeOnClose && openedWithSkillMode && SkillIconModeView.CurrentMode == openedSkillMode)
            SkillIconModeView.ClearMode();

        openedWithSkillMode = false;
        openedSkillMode = SkillModeType.None;
    }

    private void LateUpdate()
    {
        if (pendingOpenSkillMode == SkillModeType.None)
            return;

        TryApplyPendingOpenSkill();
    }

    [ContextMenu("Apply Pattern Puzzle Variant")]
    public void ApplyVariant()
    {
        AutoWire();
        ApplyRequiredCount();
        ApplyModeLock();
        ApplyDecodeObjects();

        if (legacySkillOnOpen != null)
            legacySkillOnOpen.enabled = false;
    }

    public void SetBasic()
    {
        variant = PatternPuzzleVariant.Basic;
        ApplyVariant();
    }

    public void SetRotate()
    {
        variant = PatternPuzzleVariant.Rotate;
        ApplyVariant();
    }

    public void SetAssemble()
    {
        variant = PatternPuzzleVariant.Assemble;
        ApplyVariant();
    }

    public void SetDecode()
    {
        variant = PatternPuzzleVariant.Decode;
        ApplyVariant();
    }

    private void AutoWire()
    {
        if (!autoFindReferences)
            return;

        if (pathPuzzle == null)
            pathPuzzle = GetComponentInChildren<PathPuzzleManager>(true);

        if (modeLock == null)
            modeLock = GetComponent<PuzzleModeLock>();

        if (modeLock == null)
            modeLock = GetComponentInChildren<PuzzleModeLock>(true);

        if (legacySkillOnOpen == null)
            legacySkillOnOpen = GetComponent<PatternPuzzleSkillOnOpen>();

        if (legacySkillOnOpen == null)
            legacySkillOnOpen = GetComponentInChildren<PatternPuzzleSkillOnOpen>(true);

        if (decodeConnector == null)
            decodeConnector = GetComponent<RecognitionDecodePathPuzzleConnector>();

        if (decodeConnector == null)
            decodeConnector = GetComponentInChildren<RecognitionDecodePathPuzzleConnector>(true);

        if (autoCollectDecodeObjects && (decodeOnlyObjects == null || decodeOnlyObjects.Length == 0))
            decodeOnlyObjects = CollectDecodeObjects();
    }

    private void ApplyRequiredCount()
    {
        if (!applyBasicRequiredCount || pathPuzzle == null || variant != PatternPuzzleVariant.Basic)
            return;

        pathPuzzle.requiredCount = basicRequiredCount;
    }

    private void ApplyModeLock()
    {
        if (modeLock == null)
            return;

        bool allowRotate = AllowsRotate(variant);
        bool allowAssemble = AllowsAssemble(variant);
        bool allowDecode = AllowsDecode(variant);
        modeLock.Configure(allowRotate, allowAssemble, allowDecode, true);
    }

    private void ApplyDecodeObjects()
    {
        bool decode = variant == PatternPuzzleVariant.Decode;

        if (decodeConnector != null)
            decodeConnector.enabled = decode;

        if (decodeOnlyObjects == null)
            return;

        foreach (GameObject target in decodeOnlyObjects)
        {
            if (target == null || target == gameObject)
                continue;

            target.SetActive(decode);
        }
    }

    private void ApplyOpenSkill()
    {
        openedWithSkillMode = false;
        openedSkillMode = SkillModeType.None;

        switch (variant)
        {
            case PatternPuzzleVariant.Rotate:
                GrantAndMaybeSelect(SkillModeType.Rotate);
                break;
            case PatternPuzzleVariant.Assemble:
                GrantAndMaybeSelect(SkillModeType.Assemble);
                break;
            case PatternPuzzleVariant.Decode:
                GrantAndMaybeSelect(SkillModeType.Decode);
                break;
            case PatternPuzzleVariant.Basic:
                SkillIconModeView.ClearMode();
                break;
        }
    }

    private void GrantAndMaybeSelect(SkillModeType mode)
    {
        GrantUnlockedSkills();
        openedSkillMode = mode;
        openedWithSkillMode = true;

        if (selectSkillOnOpen)
        {
            pendingOpenSkillMode = mode;
            pendingOpenSkillFrames = OpenSkillApplyFrames;
            TryApplyPendingOpenSkill();
        }
    }

    private void TryApplyPendingOpenSkill()
    {
        if (pendingOpenSkillMode == SkillModeType.None)
            return;

        GrantUnlockedSkills();
        SkillIconModeView.SelectMode(pendingOpenSkillMode);

        if (SkillIconModeView.CurrentMode == pendingOpenSkillMode)
        {
            PlayOpenSkillSfxOnce();
            ApplyOpenHighlightDelayOnce(pendingOpenSkillMode);
            pendingOpenSkillMode = SkillModeType.None;
            return;
        }

        pendingOpenSkillFrames--;
        if (pendingOpenSkillFrames > 0)
            return;

        Debug.LogWarning(
            $"[PatternPuzzleVariant] Failed to auto-select {pendingOpenSkillMode}. " +
            $"current={SkillIconModeView.CurrentMode}, " +
            $"stageAllowed={SkillModeStageRules.IsAllowed(pendingOpenSkillMode)}, " +
            $"lockAllowed={PuzzleModeLock.IsAllowedByActiveLocks(pendingOpenSkillMode)}, " +
            $"interactionLocked={SkillInteractionLock.IsLocked}",
            this);
        pendingOpenSkillMode = SkillModeType.None;
    }

    private void PlayOpenSkillSfxOnce()
    {
        if (playedOpenSkillSfx)
            return;

        playedOpenSkillSfx = true;
        GameEvent.ESFXPlay?.Invoke(OpenSkillSfxId);
    }

    private void ApplyOpenHighlightDelayOnce(SkillModeType mode)
    {
        if (delayedOpenHighlight)
            return;

        if (openHighlightDelay <= 0f)
            return;

        delayedOpenHighlight = true;

        SkillHighlightTarget[] highlights = GetComponentsInChildren<SkillHighlightTarget>(true);
        foreach (SkillHighlightTarget highlight in highlights)
        {
            if (highlight != null)
                highlight.DelayHighlight(mode, openHighlightDelay);
        }
    }

    private void GrantUnlockedSkills()
    {
        if (AllowsRotate(variant))
            SkillModeStageRules.Grant(SkillModeType.Rotate);

        if (AllowsAssemble(variant))
            SkillModeStageRules.Grant(SkillModeType.Assemble);

        if (AllowsDecode(variant))
            SkillModeStageRules.Grant(SkillModeType.Decode);
    }

    private static bool AllowsRotate(PatternPuzzleVariant puzzleVariant)
    {
        return puzzleVariant == PatternPuzzleVariant.Rotate
            || puzzleVariant == PatternPuzzleVariant.Assemble
            || puzzleVariant == PatternPuzzleVariant.Decode;
    }

    private static bool AllowsAssemble(PatternPuzzleVariant puzzleVariant)
    {
        return puzzleVariant == PatternPuzzleVariant.Assemble
            || puzzleVariant == PatternPuzzleVariant.Decode;
    }

    private static bool AllowsDecode(PatternPuzzleVariant puzzleVariant)
    {
        return puzzleVariant == PatternPuzzleVariant.Decode;
    }

    private GameObject[] CollectDecodeObjects()
    {
        List<GameObject> objects = new List<GameObject>();

        RecognitionDecodeAreaController[] areas = GetComponentsInChildren<RecognitionDecodeAreaController>(true);
        foreach (RecognitionDecodeAreaController area in areas)
            AddDecodeObject(objects, area != null ? area.gameObject : null);

        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "RecognitionDecodeInventoryController")
                AddDecodeObject(objects, behaviour.gameObject);
        }

        return objects.ToArray();
    }

    private void AddDecodeObject(List<GameObject> objects, GameObject target)
    {
        if (target == null || target == gameObject || objects.Contains(target))
            return;

        objects.Add(target);
    }
}
