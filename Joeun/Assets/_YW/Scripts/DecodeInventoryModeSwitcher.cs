using UnityEngine;

[DisallowMultipleComponent]
public sealed class DecodeInventoryModeSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject normalInventoryRoot;
    [SerializeField] private GameObject decodeInventoryRoot;
    [SerializeField] private string normalInventoryName = "Inventory Panel";
    [SerializeField] private string decodeInventoryName = "DecodeInvenPanel";
    [SerializeField] private bool hideNormalInventoryInDecode = true;
    [SerializeField] private bool hideDecodeInventoryOutsideDecode = true;

    private void Awake()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        Apply(SkillIconModeView.CurrentMode);
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
    }

    public void ApplyCurrentMode()
    {
        Apply(SkillIconModeView.CurrentMode);
    }

    void HandleSkillModeChanged(SkillModeType mode)
    {
        Apply(mode);
    }

    void Apply(SkillModeType mode)
    {
        AutoWire();

        bool decodeMode = mode == SkillModeType.Decode;

        if (normalInventoryRoot != null && hideNormalInventoryInDecode)
            normalInventoryRoot.SetActive(!decodeMode);

        if (decodeInventoryRoot != null)
        {
            if (hideDecodeInventoryOutsideDecode)
                decodeInventoryRoot.SetActive(decodeMode);
            else if (decodeMode)
                decodeInventoryRoot.SetActive(true);
        }
    }

    void AutoWire()
    {
        if (normalInventoryRoot == null && !string.IsNullOrWhiteSpace(normalInventoryName))
            normalInventoryRoot = FindInSwitcherScope(normalInventoryName);

        if (decodeInventoryRoot == null && !string.IsNullOrWhiteSpace(decodeInventoryName))
            decodeInventoryRoot = FindInSwitcherScope(decodeInventoryName);
    }

    GameObject FindInSwitcherScope(string targetName)
    {
        GameObject found = FindChildByName(transform, targetName);
        if (found != null)
            return found;

        if (transform.root != transform)
        {
            found = FindChildByName(transform.root, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    static GameObject FindChildByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == targetName)
                return child.gameObject;
        }

        return null;
    }
}
