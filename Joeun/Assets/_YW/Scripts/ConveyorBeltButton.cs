using UnityEngine;

/// <summary>
/// Click proxy for a conveyor belt button. Attach this to the same object that
/// owns the clickable Collider2D, then assign the conveyor controller.
/// </summary>
[DisallowMultipleComponent]
public class ConveyorBeltButton : MonoBehaviour, IInteractive
{
    [SerializeField] ConveyorBeltController controller;

    void Reset()
    {
        controller = GetComponentInParent<ConveyorBeltController>();
    }

    void OnValidate()
    {
        if (controller == null)
            controller = GetComponentInParent<ConveyorBeltController>();
    }

    public void Interact()
    {
        Press();
    }

    public void Press()
    {
        if (controller != null)
        {
            controller.PressButton();
            return;
        }

        Debug.LogWarning($"{nameof(ConveyorBeltButton)} needs a conveyor controller.", this);
    }
}
