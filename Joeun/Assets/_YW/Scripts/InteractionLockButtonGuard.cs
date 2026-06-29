using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionLockButtonGuard : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private bool interactableWhenUnlocked = true;

    private void Reset()
    {
        targetButton = GetComponent<Button>();
    }

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;
        Apply();
    }

    private void OnDisable()
    {
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;
    }

    private void HandleInteractionLockChanged(bool locked)
    {
        Apply();
    }

    private void Apply()
    {
        if (targetButton == null)
            return;

        targetButton.interactable = !SkillInteractionLock.IsLocked && interactableWhenUnlocked;
    }
}
