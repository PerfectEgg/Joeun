using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

[DisallowMultipleComponent]
public class RecognitionDecodePathPuzzleConnector : MonoBehaviour
{
    [SerializeField] PathPuzzleManager pathPuzzle;
    [SerializeField] RecognitionDecodeAreaController[] decodeAreas;
    [SerializeField] bool selectDecodeModeOnSuccess = true;
    [SerializeField, Min(0f)] float decodeReadyDelay = 0.4f;
    [SerializeField, Min(0f)] float successSettleDelay = 0.5f;
    [SerializeField, HideInInspector] GameObject decodeInventoryRoot;
    [SerializeField, HideInInspector] bool openDecodeInventoryOnSuccess = true;

    [FormerlySerializedAs("decodeArea")]
    [SerializeField, HideInInspector] RecognitionDecodeAreaController legacyDecodeArea;
    [FormerlySerializedAs("decodeInventory")]
    [SerializeField, HideInInspector] MonoBehaviour legacyDecodeInventory;

    Coroutine successRoutine;

    void Awake()
    {
        AutoWire();
    }

    void OnEnable()
    {
        AutoWire();

        if (pathPuzzle != null)
        {
            pathPuzzle.onSuccess.RemoveListener(HandlePathPuzzleSuccess);
            pathPuzzle.onSuccess.AddListener(HandlePathPuzzleSuccess);

            if (pathPuzzle.IsSolved)
                HandlePathPuzzleSuccess();
        }
    }

    void OnDisable()
    {
        if (pathPuzzle != null)
            pathPuzzle.onSuccess.RemoveListener(HandlePathPuzzleSuccess);

        if (successRoutine != null)
        {
            StopCoroutine(successRoutine);
            successRoutine = null;
        }
    }

    public void HandlePathPuzzleSuccess()
    {
        if (successRoutine != null)
            StopCoroutine(successRoutine);

        successRoutine = StartCoroutine(HandlePathPuzzleSuccessRoutine());
    }

    IEnumerator HandlePathPuzzleSuccessRoutine()
    {
        float delay = decodeReadyDelay + successSettleDelay;
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        MarkDecodeAreasReady();

        if (openDecodeInventoryOnSuccess && decodeInventoryRoot != null)
            decodeInventoryRoot.SetActive(true);

        if (selectDecodeModeOnSuccess)
        {
            SkillModeStageRules.Grant(SkillModeType.Decode);
            SkillIconModeView.SelectMode(SkillModeType.Decode);
        }

        successRoutine = null;
    }

    void AutoWire()
    {
        if (pathPuzzle == null)
            pathPuzzle = GetComponentInChildren<PathPuzzleManager>(true);

        if (decodeAreas == null || decodeAreas.Length == 0)
            decodeAreas = GetComponentsInChildren<RecognitionDecodeAreaController>(true);

        if (legacyDecodeArea != null && (decodeAreas == null || decodeAreas.Length == 0))
            decodeAreas = new[] { legacyDecodeArea };

        if (decodeInventoryRoot == null && legacyDecodeInventory != null)
            decodeInventoryRoot = legacyDecodeInventory.gameObject;
    }

    void MarkDecodeAreasReady()
    {
        if (decodeAreas == null)
            return;

        foreach (RecognitionDecodeAreaController area in decodeAreas)
        {
            if (area == null)
                continue;

            area.MarkDecodeReady();
        }
    }
}
