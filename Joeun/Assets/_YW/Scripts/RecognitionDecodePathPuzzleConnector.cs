using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class RecognitionDecodePathPuzzleConnector : MonoBehaviour
{
    [SerializeField] PathPuzzleManager pathPuzzle;
    [SerializeField] RecognitionDecodeAreaController[] decodeAreas;
    [SerializeField] bool selectDecodeModeOnSuccess = true;
    [SerializeField, HideInInspector] GameObject decodeInventoryRoot;
    [SerializeField, HideInInspector] bool openDecodeInventoryOnSuccess = true;

    [FormerlySerializedAs("decodeArea")]
    [SerializeField, HideInInspector] RecognitionDecodeAreaController legacyDecodeArea;
    [FormerlySerializedAs("decodeInventory")]
    [SerializeField, HideInInspector] MonoBehaviour legacyDecodeInventory;

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
        }
    }

    void OnDisable()
    {
        if (pathPuzzle != null)
            pathPuzzle.onSuccess.RemoveListener(HandlePathPuzzleSuccess);
    }

    public void HandlePathPuzzleSuccess()
    {
        MarkDecodeAreasReady();

        if (openDecodeInventoryOnSuccess && decodeInventoryRoot != null)
            decodeInventoryRoot.SetActive(true);

        if (selectDecodeModeOnSuccess)
        {
            SkillModeStageRules.Grant(SkillModeType.Decode);
            SkillIconModeView.SelectMode(SkillModeType.Decode);
        }
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
