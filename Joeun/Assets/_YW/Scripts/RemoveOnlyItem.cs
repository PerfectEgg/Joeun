using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class RemoveOnlyItem : MonoBehaviour, IInteractive, IHoverable
{
    [Header("Feedback")]
    [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.12f;
    [SerializeField, Min(0.01f)] private float removeDuration = 0.28f;
    [SerializeField] private Vector2 removeLocalOffset = new Vector2(0.12f, -0.35f);
    [SerializeField] private float removeRotation = -18f;
    [SerializeField, Min(0f)] private float liftHeight = 0.06f;
    [SerializeField] private string removeSfxId = "";

    [Header("Events")]
    [SerializeField] private UnityEvent onRemoved;

    private SpriteRenderer spriteRenderer;
    private Collider2D[] colliders;
    private Vector3 originLocalPosition;
    private Vector3 originLocalScale;
    private Quaternion originLocalRotation;
    private Color originColor = Color.white;
    private Coroutine removeRoutine;
    private bool isRemoving;
    private bool isRemoved;
    private bool removedEventInvoked;
    private bool suppressHoverUntilPointerExit;
    private bool cached;

    private void Awake()
    {
        CacheInitialState();
    }

    private void OnEnable()
    {
        CacheInitialState();

        if (isRemoved)
        {
            gameObject.SetActive(false);
            return;
        }

        RestoreInitialState();
        suppressHoverUntilPointerExit = IsPointerAlreadyOverItem();
    }

    private void OnDisable()
    {
        if (!isRemoved)
            return;

        if (removeRoutine != null)
        {
            StopCoroutine(removeRoutine);
            removeRoutine = null;
        }

        SetCollidersEnabled(false);
    }

    public void Interact()
    {
        if (isRemoving || isRemoved)
            return;

        isRemoved = true;
        isRemoving = true;
        SetCollidersEnabled(false);

        if (!string.IsNullOrWhiteSpace(removeSfxId))
            GameEvent.ESFXPlay?.Invoke(removeSfxId);

        InvokeRemovedEvent();

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        removeRoutine = StartCoroutine(RemoveRoutine());
    }

    public void OnHoverEnter()
    {
        if (isRemoving || suppressHoverUntilPointerExit)
            return;

        transform.localScale = originLocalScale * hoverScaleMultiplier;
    }

    public void OnHoverExit()
    {
        if (isRemoving)
            return;

        suppressHoverUntilPointerExit = false;
        transform.localScale = originLocalScale;
    }

    private IEnumerator RemoveRoutine()
    {
        Vector3 startPosition = transform.localPosition;
        Vector3 endPosition = originLocalPosition + (Vector3)removeLocalOffset;
        Vector3 startScale = transform.localScale;
        Vector3 popScale = originLocalScale * 1.06f;
        Quaternion startRotation = transform.localRotation;
        Quaternion endRotation = originLocalRotation * Quaternion.Euler(0f, 0f, removeRotation);
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float elapsed = 0f;
        while (elapsed < removeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / removeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float lift = Mathf.Sin(t * Mathf.PI) * liftHeight;
            Vector3 position = Vector3.LerpUnclamped(startPosition, endPosition, eased);
            position.y += lift;

            transform.localPosition = position;
            transform.localRotation = Quaternion.SlerpUnclamped(startRotation, endRotation, eased);
            transform.localScale = Vector3.LerpUnclamped(startScale, popScale, Mathf.Sin(t * Mathf.PI));

            if (spriteRenderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, eased);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        removeRoutine = null;
        gameObject.SetActive(false);
    }

    private void CacheInitialState()
    {
        if (cached)
            return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        colliders = GetComponents<Collider2D>();
        originLocalPosition = transform.localPosition;
        originLocalScale = transform.localScale;
        originLocalRotation = transform.localRotation;

        if (spriteRenderer != null)
            originColor = spriteRenderer.color;

        cached = true;
    }

    private void RestoreInitialState()
    {
        isRemoving = false;
        transform.localPosition = originLocalPosition;
        transform.localScale = originLocalScale;
        transform.localRotation = originLocalRotation;

        if (spriteRenderer != null)
            spriteRenderer.color = originColor;

        SetCollidersEnabled(true);
    }

    private void InvokeRemovedEvent()
    {
        if (removedEventInvoked)
            return;

        removedEventInvoked = true;
        onRemoved?.Invoke();
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null)
            return;

        foreach (Collider2D itemCollider in colliders)
        {
            if (itemCollider != null)
                itemCollider.enabled = enabled;
        }
    }

    private bool IsPointerAlreadyOverItem()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Transform hitTransform = hit.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return true;
        }

        return false;
    }
}
