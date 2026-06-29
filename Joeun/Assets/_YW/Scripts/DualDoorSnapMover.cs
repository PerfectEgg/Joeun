using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DualDoorSnapMover : MonoBehaviour
{
    [System.Serializable]
    private class DoorMove
    {
        public Transform door = null;
        public Transform openSnapPoint = null;

        [HideInInspector] public Vector3 closedPosition;
        [HideInInspector] public bool hasClosedPosition;

        public bool IsValid => door != null && openSnapPoint != null;
    }

    [Header("Doors")]
    [SerializeField] private DoorMove leftDoor = new DoorMove();
    [SerializeField] private DoorMove rightDoor = new DoorMove();

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float duration = 0.35f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool ignoreRepeatedOpen = true;

    [Header("After Open")]
    [SerializeField] private GameObject[] activateAfterOpen;
    [SerializeField] private GameObject[] deactivateAfterOpen;
    [SerializeField] private Collider2D[] enableCollidersAfterOpen;
    [SerializeField] private Collider2D[] disableCollidersAfterOpen;

    private Coroutine moveRoutine;
    private bool isOpen;

    private void Awake()
    {
        CaptureClosedPositions();
    }

    private void OnDisable()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    [ContextMenu("Open Doors")]
    public void OpenDoors()
    {
        if (ignoreRepeatedOpen && isOpen)
            return;

        CaptureClosedPositions();

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(OpenRoutine());
    }

    [ContextMenu("Open Doors Immediate")]
    public void OpenDoorsImmediate()
    {
        CaptureClosedPositions();
        MoveImmediate(leftDoor, true);
        MoveImmediate(rightDoor, true);
        ApplyAfterOpen();
        isOpen = true;
    }

    [ContextMenu("Reset Closed Immediate")]
    public void ResetClosedImmediate()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        MoveImmediate(leftDoor, false);
        MoveImmediate(rightDoor, false);
        isOpen = false;
    }

    private IEnumerator OpenRoutine()
    {
        Vector3 leftStart = GetDoorPosition(leftDoor);
        Vector3 rightStart = GetDoorPosition(rightDoor);
        Vector3 leftTarget = GetOpenPosition(leftDoor);
        Vector3 rightTarget = GetOpenPosition(rightDoor);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            MoveDoor(leftDoor, Vector3.LerpUnclamped(leftStart, leftTarget, t));
            MoveDoor(rightDoor, Vector3.LerpUnclamped(rightStart, rightTarget, t));
            yield return null;
        }

        MoveDoor(leftDoor, leftTarget);
        MoveDoor(rightDoor, rightTarget);

        ApplyAfterOpen();
        isOpen = true;
        moveRoutine = null;
    }

    private void CaptureClosedPositions()
    {
        CaptureClosedPosition(leftDoor);
        CaptureClosedPosition(rightDoor);
    }

    private static void CaptureClosedPosition(DoorMove move)
    {
        if (move == null || move.door == null || move.hasClosedPosition)
            return;

        move.closedPosition = move.door.position;
        move.hasClosedPosition = true;
    }

    private static Vector3 GetDoorPosition(DoorMove move)
    {
        return move != null && move.door != null ? move.door.position : Vector3.zero;
    }

    private static Vector3 GetOpenPosition(DoorMove move)
    {
        if (move == null)
            return Vector3.zero;

        return move.IsValid ? move.openSnapPoint.position : GetDoorPosition(move);
    }

    private static void MoveDoor(DoorMove move, Vector3 position)
    {
        if (move != null && move.door != null)
            move.door.position = position;
    }

    private static void MoveImmediate(DoorMove move, bool open)
    {
        if (move == null || move.door == null)
            return;

        if (open && move.openSnapPoint != null)
        {
            move.door.position = move.openSnapPoint.position;
            return;
        }

        if (!open && move.hasClosedPosition)
            move.door.position = move.closedPosition;
    }

    private void ApplyAfterOpen()
    {
        SetObjectsActive(activateAfterOpen, true);
        SetObjectsActive(deactivateAfterOpen, false);
        SetCollidersEnabled(enableCollidersAfterOpen, true);
        SetCollidersEnabled(disableCollidersAfterOpen, false);
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;

        foreach (GameObject target in targets)
        {
            if (target != null)
                target.SetActive(active);
        }
    }

    private static void SetCollidersEnabled(Collider2D[] targets, bool enabled)
    {
        if (targets == null)
            return;

        foreach (Collider2D target in targets)
        {
            if (target != null)
                target.enabled = enabled;
        }
    }
}
