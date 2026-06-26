using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Keeps a PuzzlePart and its joint overlay canvases in one render-order group.
/// PuzzlePart still owns drag, rotate, snap, and connection logic.
/// </summary>
[DisallowMultipleComponent]
public class PuzzlePartRenderOrderController : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    const int SortingOrderOffset = 10;
    const int SortingOrderStep = 4;
    const int ConnectedJointOverlayOrderOffset = SortingOrderStep + 2;
    const int PressedPartOrderOffset = 70;

    static PuzzlePartRenderOrderController pressedPart;

    [Header("Installer")]
    [SerializeField] Transform installRoot;
    bool installChildPuzzleParts = true;

    [Header("Render Order")]
    [Tooltip("Back-to-front order. Empty uses the default arm order: Arm3, Arm4, Arm2, Arm1.")]
    [SerializeField] PuzzlePart[] visualOrderBackToFront;
    bool useDefaultArmVisualOrder = true;
    bool suppressRotateModeOnPuzzlePartInput = true;

    PuzzlePart puzzlePart;
    PuzzlePartRenderOrderController installerController;
    Canvas rootCanvas;
    Canvas partCanvas;
    GraphicRaycaster partRaycaster;
    bool refreshedCanvasAfterStart;

    public static void InstallUnder(Transform root)
    {
        InstallUnder(root, null);
    }

    static void InstallUnder(Transform root, PuzzlePartRenderOrderController installer)
    {
        if (root == null) return;

        PuzzlePart[] parts = root.GetComponentsInChildren<PuzzlePart>(true);
        foreach (var part in parts)
            InstallOn(part, installer);

        RefreshParents(parts);
    }

    public static void InstallForParts(IList<PuzzlePart> parts)
    {
        if (parts == null) return;

        foreach (var part in parts)
            InstallOn(part, null);

        RefreshParents(parts);
    }

    public static void RefreshParents(IList<PuzzlePart> parts)
    {
        if (parts == null) return;

        var refreshedParents = new HashSet<Transform>();
        foreach (var part in parts)
        {
            if (part == null || part.transform.parent == null)
                continue;

            if (refreshedParents.Add(part.transform.parent))
                NormalizeSiblingSorting(part.transform.parent);
        }
    }

    static void InstallOn(PuzzlePart part, PuzzlePartRenderOrderController installer)
    {
        if (part == null) return;

        if (!part.TryGetComponent(out PuzzlePartRenderOrderController controller))
            controller = part.gameObject.AddComponent<PuzzlePartRenderOrderController>();

        controller.installerController = installer;
    }

    void Awake()
    {
        puzzlePart = GetComponent<PuzzlePart>();

        if (IsPartController)
            RefreshRootCanvas();
        else
            InstallChildControllers();
    }

    void OnEnable()
    {
        if (!IsPartController)
        {
            InstallChildControllers();
            return;
        }

        RefreshRootCanvas();
        NormalizeSiblingSorting(transform.parent);
    }

    void Start()
    {
        if (!IsPartController)
        {
            InstallChildControllers();
            return;
        }

        RefreshRootCanvas();
        NormalizeSiblingSorting(transform.parent);
    }

    void LateUpdate()
    {
        if (!IsPartController)
            return;

        if (refreshedCanvasAfterStart)
            return;

        refreshedCanvasAfterStart = true;
        RefreshRootCanvas();
        NormalizeSiblingSorting(transform.parent);
    }

    void OnDisable()
    {
        if (pressedPart == this)
            pressedPart = null;

        refreshedCanvasAfterStart = false;
    }

    void OnTransformParentChanged()
    {
        if (!IsPartController)
        {
            InstallChildControllers();
            return;
        }

        RefreshRootCanvas();
        NormalizeSiblingSorting(transform.parent);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPartController)
            return;

        if (ShowCompletedAssembly.IsReadyAssemblyClick(eventData.position))
            return;

        SuppressRotateModeIfNeeded();
        pressedPart = this;
        RefreshOrder();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pressedPart == this)
        {
            pressedPart = null;
            NormalizeSiblingSorting(transform.parent);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsPartController)
            return;

        if (ShowCompletedAssembly.IsReadyAssemblyClick(eventData.position))
            return;

        SuppressRotateModeIfNeeded();
        pressedPart = this;
        RefreshOrder();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pressedPart == this)
        {
            pressedPart = null;
            NormalizeSiblingSorting(transform.parent);
        }
    }

    bool IsPartController => puzzlePart != null;

    void SuppressRotateModeIfNeeded()
    {
        PuzzlePartRenderOrderController source = installerController != null ? installerController : this;
        if (!source.suppressRotateModeOnPuzzlePartInput)
            return;

        if (SkillIconModeView.CurrentMode == SkillModeType.Rotate)
            SkillIconModeView.ClearMode();

        PuzzleModeManager modeManager = PuzzleModeManager.Instance;
        if (modeManager != null && modeManager.IsRotate)
            modeManager.SetMode(PuzzleModeManager.Mode.None);
    }

    void InstallChildControllers()
    {
        if (!installChildPuzzleParts)
            return;

        Transform root = installRoot != null ? installRoot : transform;
        InstallUnder(root, this);
    }

    void RefreshOrder()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        NormalizeSiblingSorting(parent);
    }

    void ApplySortingOrder(int sortingOrder)
    {
        EnsureSortingCanvas();

        partCanvas.sortingOrder = sortingOrder;
        int connectedJointOverlayOrder = sortingOrder + ConnectedJointOverlayOrderOffset;

        Canvas[] childCanvases = GetComponentsInChildren<Canvas>(true);
        foreach (var childCanvas in childCanvases)
        {
            if (childCanvas == null || childCanvas == partCanvas)
                continue;

            CopyCanvasSettings(rootCanvas, childCanvas);
            childCanvas.overrideSorting = true;

            if (IsConnectedJointOverlay(childCanvas))
                childCanvas.sortingOrder = connectedJointOverlayOrder;
            else
                childCanvas.sortingOrder = sortingOrder + 1;
        }
    }

    void EnsureSortingCanvas()
    {
        if (partCanvas == null)
            partCanvas = GetComponent<Canvas>();

        if (partCanvas == null)
            partCanvas = gameObject.AddComponent<Canvas>();

        RefreshRootCanvas();
        CopyCanvasSettings(rootCanvas, partCanvas);
        partCanvas.overrideSorting = true;

        if (partRaycaster == null)
            partRaycaster = GetComponent<GraphicRaycaster>();

        if (partRaycaster == null)
            partRaycaster = gameObject.AddComponent<GraphicRaycaster>();
    }

    void RefreshRootCanvas()
    {
        rootCanvas = FindParentCanvas();
    }

    Canvas FindParentCanvas()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.TryGetComponent(out Canvas canvas))
                return canvas;

            current = current.parent;
        }

        return null;
    }

    static void NormalizeSiblingSorting(Transform parent)
    {
        if (parent == null) return;

        Canvas parentCanvas = parent.GetComponentInParent<Canvas>();
        int baseSortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + SortingOrderOffset;
        int pressedPartSortingOrder = baseSortingOrder + PressedPartOrderOffset;
        var controllers = new List<PuzzlePartRenderOrderController>();

        for (int i = 0; i < parent.childCount; i++)
        {
            if (!parent.GetChild(i).TryGetComponent(out PuzzlePartRenderOrderController controller)
                || !controller.isActiveAndEnabled
                || !controller.IsPartController)
            {
                continue;
            }

            controllers.Add(controller);
        }

        controllers.Sort(CompareControllers);

        for (int i = 0; i < controllers.Count; i++)
        {
            PuzzlePartRenderOrderController controller = controllers[i];
            int sortingOrder = controller == pressedPart
                ? pressedPartSortingOrder
                : baseSortingOrder + i * SortingOrderStep;

            controller.ApplySortingOrder(sortingOrder);
        }
    }

    static int CompareControllers(PuzzlePartRenderOrderController a, PuzzlePartRenderOrderController b)
    {
        if (a == b)
            return 0;

        int orderCompare = a.VisualOrder.CompareTo(b.VisualOrder);
        if (orderCompare != 0)
            return orderCompare;

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    int VisualOrder
    {
        get
        {
            PuzzlePartRenderOrderController source = installerController != null ? installerController : this;

            int manualIndex = source.IndexInManualOrder(puzzlePart);
            if (manualIndex >= 0)
                return manualIndex;

            if (source.useDefaultArmVisualOrder)
                return GetDefaultArmVisualOrder(puzzlePart);

            return puzzlePart != null ? puzzlePart.orderIndex : 0;
        }
    }

    int IndexInManualOrder(PuzzlePart part)
    {
        if (visualOrderBackToFront == null || part == null)
            return -1;

        for (int i = 0; i < visualOrderBackToFront.Length; i++)
        {
            if (visualOrderBackToFront[i] == part)
                return i;
        }

        return -1;
    }

    static int GetDefaultArmVisualOrder(PuzzlePart part)
    {
        if (part == null)
            return 0;

        string partId = part.partId != null ? part.partId.Trim().ToUpperInvariant() : string.Empty;
        string objectName = part.name.ToUpperInvariant();

        if (partId == "C" || objectName.Contains("ARM3"))
            return 0;

        if (partId == "D" || objectName.Contains("ARM4"))
            return 1;

        if (partId == "B" || objectName.Contains("ARM2"))
            return 2;

        if (partId == "A" || objectName.Contains("ARM1"))
            return 3;

        return part.orderIndex;
    }

    static void CopyCanvasSettings(Canvas source, Canvas target)
    {
        if (source == null || target == null)
            return;

        target.renderMode = source.renderMode;
        target.worldCamera = source.worldCamera;
        target.planeDistance = source.planeDistance;
        target.pixelPerfect = source.pixelPerfect;
        target.sortingLayerID = source.sortingLayerID;
        target.additionalShaderChannels = source.additionalShaderChannels;
    }

    static bool IsConnectedJointOverlay(Canvas canvas)
    {
        string objectName = canvas != null ? canvas.name.ToLowerInvariant() : string.Empty;
        return objectName.Contains("joint_conected")
            || objectName.Contains("joint_connected")
            || objectName.Contains("joint_contected");
    }
}
