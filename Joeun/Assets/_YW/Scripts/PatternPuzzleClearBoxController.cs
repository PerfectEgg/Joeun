using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class PatternPuzzleClearBoxController : MonoBehaviour, IInteractive, IPointerClickHandler
{
    [Header("Box State")]
    [SerializeField] private RectTransform slidingRoot;
    [SerializeField] private RectTransform slideAnchor;
    [SerializeField] private GameObject unclearBox;
    [SerializeField] private GameObject clearBox;
    [SerializeField] private Collider2D clearButtonCollider;
    [SerializeField] private RectTransform clearButtonRect;

    [Header("After Slide")]
    [SerializeField] private Collider2D[] enableCollidersAfterSlide;
    [SerializeField] private GameObject[] activateObjectsAfterSlide;
    [SerializeField] private GameObject[] deactivateObjectsAfterSlide;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float slideDuration = 0.35f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool requireClearBeforeSlide = true;
    [SerializeField] private bool useRectClickFallback = true;

    private Coroutine slideRoutine;
    private bool isCleared;
    private bool isOpen;
    private bool hasClosedPosition;
    private Vector2 closedPosition;

    private void Reset()
    {
        AutoWireSelf();
    }

    private void OnValidate()
    {
        AutoWireSelf();
    }

    private void Awake()
    {
        AutoWireSelf();
        CacheClosedPosition();

        if (initializeOnAwake)
            ApplyUnclearedState();
    }

    private void Update()
    {
        if (!useRectClickFallback || !Input.GetMouseButtonDown(0))
            return;

        if (clearButtonCollider == null || !clearButtonCollider.enabled || clearButtonRect == null)
            return;

        if (RectTransformUtility.RectangleContainsScreenPoint(clearButtonRect, Input.mousePosition, GetEventCamera()))
            Press();
    }

    private void OnDisable()
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;

            if (!isOpen)
            {
                RestoreClosedPosition();
                SetColliderEnabled(clearButtonCollider, isCleared || !requireClearBeforeSlide);
            }
        }
    }

    public void HandlePuzzleCleared()
    {
        isCleared = true;

        if (unclearBox != null)
            unclearBox.SetActive(false);

        if (clearBox != null)
            clearBox.SetActive(true);

        SetColliderEnabled(clearButtonCollider, true);
    }

    public void Interact()
    {
        Press();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        Press();
    }

    private void OnMouseDown()
    {
        Press();
    }

    public void Press()
    {
        if (isOpen || slideRoutine != null)
            return;

        if (requireClearBeforeSlide && !isCleared && !IsClearButtonEnabled())
            return;

        if (slidingRoot == null || slideAnchor == null)
            return;

        GameEvent.ESFXPlay?.Invoke("Pattern_Recognition_Open");
        SetColliderEnabled(clearButtonCollider, false);
        slideRoutine = StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        Vector2 start = slidingRoot.anchoredPosition;
        Vector2 target = slideAnchor.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += delta;

            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = t * t * (3f - 2f * t);
            slidingRoot.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        slidingRoot.anchoredPosition = target;
        isOpen = true;
        slideRoutine = null;

        SetCollidersEnabled(enableCollidersAfterSlide, true);
        SetObjectsActive(deactivateObjectsAfterSlide, false);
        SetObjectsActive(activateObjectsAfterSlide, true);
    }

    private void ApplyUnclearedState()
    {
        isCleared = false;
        isOpen = false;
        RestoreClosedPosition();

        if (unclearBox != null)
            unclearBox.SetActive(true);

        if (clearBox != null)
            clearBox.SetActive(false);

        SetColliderEnabled(clearButtonCollider, false);
        SetCollidersEnabled(enableCollidersAfterSlide, false);
        SetObjectsActive(activateObjectsAfterSlide, false);
        SetObjectsActive(deactivateObjectsAfterSlide, true);
    }

    private bool IsClearButtonEnabled()
    {
        return clearButtonCollider != null && clearButtonCollider.enabled;
    }

    private void AutoWireSelf()
    {
        if (clearButtonRect == null)
            clearButtonRect = transform as RectTransform;

        if (clearButtonCollider == null)
            clearButtonCollider = GetComponent<Collider2D>();
    }

    private void CacheClosedPosition()
    {
        if (hasClosedPosition || slidingRoot == null)
            return;

        closedPosition = slidingRoot.anchoredPosition;
        hasClosedPosition = true;
    }

    private void RestoreClosedPosition()
    {
        CacheClosedPosition();

        if (hasClosedPosition && slidingRoot != null)
            slidingRoot.anchoredPosition = closedPosition;
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }

    private static void SetColliderEnabled(Collider2D target, bool enabled)
    {
        if (target != null)
            target.enabled = enabled;
    }

    private static void SetCollidersEnabled(Collider2D[] targets, bool enabled)
    {
        if (targets == null)
            return;

        foreach (Collider2D target in targets)
            SetColliderEnabled(target, enabled);
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
}
