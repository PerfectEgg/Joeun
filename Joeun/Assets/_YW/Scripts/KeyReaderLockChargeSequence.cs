using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class KeyReaderLockChargeSequence : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameObject charged;
    [SerializeField] private Transform chargedTarget;

    [Header("Reader Swap")]
    [SerializeField] private GameObject keyReaderLock;
    [SerializeField] private GameObject keyReader;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float moveDuration = 0.45f;
    [SerializeField, Min(0f)] private float settleDelay = 0.1f;

    [Header("Feedback")]
    [SerializeField] private string clickSfxId = "Door_Unlocked";
    [SerializeField] private bool hideChargedOnAwake = true;
    [SerializeField] private bool disableLockCollidersWhilePlaying = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onCompleted;

    private RectTransform chargedRect;
    private RectTransform chargedTargetRect;
    private Vector3 chargedStartLocalPosition;
    private Vector2 chargedStartAnchoredPosition;
    private Coroutine sequenceRoutine;
    private bool completed;

    private void Awake()
    {
        CacheStartState();

        if (hideChargedOnAwake && charged != null)
            charged.SetActive(false);
    }

    public void Play()
    {
        if (completed || sequenceRoutine != null)
            return;

        if (charged == null || chargedTarget == null)
        {
            CompleteImmediate();
            return;
        }

        sequenceRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetLockCollidersEnabled(false);
        ShowChargedAtStart();

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            ApplyChargedPosition(eased);
            yield return null;
        }

        ApplyChargedPosition(1f);

        if (!string.IsNullOrWhiteSpace(clickSfxId))
            GameEvent.ESFXPlay?.Invoke(clickSfxId);

        if (settleDelay > 0f)
            yield return new WaitForSeconds(settleDelay);

        CompleteImmediate();
    }

    private void CompleteImmediate()
    {
        completed = true;
        sequenceRoutine = null;

        onCompleted?.Invoke();

        if (keyReader != null)
            keyReader.SetActive(true);

        if (keyReaderLock != null)
            keyReaderLock.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void CacheStartState()
    {
        if (keyReaderLock == null)
            keyReaderLock = gameObject;

        if (charged == null)
            return;

        Transform chargedTransform = charged.transform;
        chargedRect = chargedTransform as RectTransform;
        chargedTargetRect = chargedTarget as RectTransform;
        chargedStartLocalPosition = chargedTransform.localPosition;

        if (chargedRect != null)
            chargedStartAnchoredPosition = chargedRect.anchoredPosition;
    }

    private void ShowChargedAtStart()
    {
        Transform chargedTransform = charged.transform;

        if (chargedRect != null)
            chargedRect.anchoredPosition = chargedStartAnchoredPosition;
        else
            chargedTransform.localPosition = chargedStartLocalPosition;

        charged.SetActive(true);
    }

    private void ApplyChargedPosition(float t)
    {
        if (chargedRect != null && chargedTargetRect != null)
        {
            chargedRect.anchoredPosition = Vector2.LerpUnclamped(
                chargedStartAnchoredPosition,
                chargedTargetRect.anchoredPosition,
                t);
            return;
        }

        charged.transform.localPosition = Vector3.LerpUnclamped(
            chargedStartLocalPosition,
            chargedTarget.localPosition,
            t);
    }

    private void SetLockCollidersEnabled(bool enabled)
    {
        if (!disableLockCollidersWhilePlaying || keyReaderLock == null)
            return;

        Collider2D[] colliders = keyReaderLock.GetComponents<Collider2D>();
        foreach (Collider2D lockCollider in colliders)
        {
            if (lockCollider != null)
                lockCollider.enabled = enabled;
        }
    }
}
