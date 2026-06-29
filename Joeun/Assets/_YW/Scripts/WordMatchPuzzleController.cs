using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WordMatchPuzzleController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform lineRoot;
    [SerializeField] private WordMatchEndpoint[] endpoints = Array.Empty<WordMatchEndpoint>();
    [SerializeField] private LetterBankController letterBank;
    [SerializeField] private string initiallyKnownLetters = "CODE";
    [SerializeField] private bool autoCollectEndpoints = true;
    [SerializeField] private bool lockMatchedPairs = true;
    [SerializeField] private bool unlockRewardLettersImmediately;

    [Header("Line")]
    [SerializeField] private Color previewLineColor = new Color(0.75f, 0.92f, 0.86f, 0.72f);
    [SerializeField] private Color correctLineColor = new Color(0.58f, 1f, 0.44f, 0.9f);
    [SerializeField] private Color wrongLineColor = new Color(1f, 0.24f, 0.24f, 0.9f);
    [SerializeField] private float previewLineWidth = 5f;
    [SerializeField] private float lockedLineWidth = 4f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCorrectMatch;
    [SerializeField] private UnityEvent onWrongMatch;
    [SerializeField] private UnityEvent onCompleted;

    private WordMatchEndpoint dragStart;
    private WordMatchEndpoint clickStart;
    private WordMatchLineGraphic previewLine;
    private int matchedPairCount;
    private bool enforcingInitialLetters;

    private void Awake()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        AutoWire();
        SubscribeInitialLetterGuards();
        UnlockInitiallyKnownLetters();
    }

    private void OnDisable()
    {
        UnsubscribeInitialLetterGuards();
    }

    private void Start()
    {
        AutoWire();
        UnlockInitiallyKnownLetters();
    }

    public void BeginDrag(WordMatchEndpoint endpoint, PointerEventData eventData)
    {
        if (!CanStartFrom(endpoint))
            return;

        dragStart = endpoint;
        clickStart = null;
        EnsurePreviewLine();
        previewLine.gameObject.SetActive(true);
        previewLine.SetVisual(previewLineColor, previewLineWidth);
        previewLine.SetPoints(GetEndpointLocalCenter(endpoint), ScreenToLineLocal(eventData.position));
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (dragStart == null || previewLine == null)
            return;

        previewLine.SetPoints(GetEndpointLocalCenter(dragStart), ScreenToLineLocal(eventData.position));
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (dragStart == null)
            return;

        WordMatchEndpoint end = FindEndpoint(eventData);
        TryMatch(dragStart, end, true);
        ClearDragLine();
        dragStart = null;
    }

    public void HandleEndpointClick(WordMatchEndpoint endpoint, PointerEventData eventData)
    {
        if (!CanStartFrom(endpoint))
            return;

        if (clickStart == null)
        {
            clickStart = endpoint;
            return;
        }

        WordMatchEndpoint start = clickStart;
        clickStart = null;

        if (start == endpoint)
            return;

        TryMatch(start, endpoint, false);
    }

    public void ResetMatches()
    {
        matchedPairCount = 0;
        clickStart = null;
        dragStart = null;
        ClearDragLine();

        WordMatchLineGraphic[] lines = GetComponentsInChildren<WordMatchLineGraphic>(true);
        foreach (WordMatchLineGraphic line in lines)
        {
            if (line != null && line != previewLine)
                Destroy(line.gameObject);
        }

        CollectEndpointsIfNeeded();
        foreach (WordMatchEndpoint endpoint in endpoints)
        {
            if (endpoint != null)
                endpoint.ResetMatchState();
        }
    }

    private void TryMatch(WordMatchEndpoint a, WordMatchEndpoint b, bool fromDrag)
    {
        if (a == null || b == null || a == b)
        {
            PlayWrongFeedback(a, b, fromDrag);
            return;
        }

        if (!CanPair(a, b))
        {
            PlayWrongFeedback(a, b, fromDrag);
            return;
        }

        bool correct = a.MatchKey == b.MatchKey && !string.IsNullOrEmpty(a.MatchKey);
        if (!correct)
        {
            PlayWrongFeedback(a, b, fromDrag);
            return;
        }

        LockPair(a, b);
        ActivateMatchRewards(a, b);
        onCorrectMatch?.Invoke();

        if (IsCompleted())
            onCompleted?.Invoke();
    }

    private bool CanStartFrom(WordMatchEndpoint endpoint)
    {
        if (endpoint == null)
            return false;

        return !lockMatchedPairs || !endpoint.IsMatched;
    }

    private bool CanPair(WordMatchEndpoint a, WordMatchEndpoint b)
    {
        if (a.Side == b.Side)
            return false;

        if (!lockMatchedPairs)
            return true;

        return !a.IsMatched && !b.IsMatched;
    }

    private void LockPair(WordMatchEndpoint a, WordMatchEndpoint b)
    {
        a.SetMatched(true);
        b.SetMatched(true);
        matchedPairCount++;

        WordMatchLineGraphic line = CreateLine("__WordMatchLine");
        line.SetVisual(correctLineColor, lockedLineWidth);
        line.SetPoints(GetEndpointLocalCenter(a), GetEndpointLocalCenter(b));
    }

    private void UnlockRewardLetters(WordMatchEndpoint a, WordMatchEndpoint b)
    {
        if (letterBank == null)
            letterBank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);

        if (letterBank == null)
            return;

        letterBank.UnlockLetters(a.RewardLetters);
        letterBank.UnlockLetters(b.RewardLetters);
    }

    private void ActivateMatchRewards(WordMatchEndpoint a, WordMatchEndpoint b)
    {
        a.ActivateDecodeRewards();
        b.ActivateDecodeRewards();

        if (unlockRewardLettersImmediately)
            UnlockRewardLetters(a, b);
    }

    private bool IsCompleted()
    {
        int expectedPairs = 0;
        CollectEndpointsIfNeeded();

        foreach (WordMatchEndpoint endpoint in endpoints)
        {
            if (endpoint != null && endpoint.Side == WordMatchEndpointSide.Source)
                expectedPairs++;
        }

        return expectedPairs > 0 && matchedPairCount >= expectedPairs;
    }

    private void PlayWrongFeedback(WordMatchEndpoint a, WordMatchEndpoint b, bool fromDrag)
    {
        a?.PlayInvalidFeedback();
        b?.PlayInvalidFeedback();
        onWrongMatch?.Invoke();

        if (!fromDrag && a != null && b != null)
            StartCoroutine(WrongLineRoutine(a, b));
    }

    private IEnumerator WrongLineRoutine(WordMatchEndpoint a, WordMatchEndpoint b)
    {
        WordMatchLineGraphic line = CreateLine("__WordMatchWrongLine");
        line.SetVisual(wrongLineColor, previewLineWidth);
        line.SetPoints(GetEndpointLocalCenter(a), GetEndpointLocalCenter(b));

        yield return new WaitForSecondsRealtime(0.14f);

        if (line != null)
            Destroy(line.gameObject);
    }

    private void ClearDragLine()
    {
        if (previewLine != null)
            previewLine.gameObject.SetActive(false);
    }

    private void EnsurePreviewLine()
    {
        if (previewLine != null)
            return;

        previewLine = CreateLine("__WordMatchPreviewLine");
        previewLine.gameObject.SetActive(false);
    }

    private WordMatchLineGraphic CreateLine(string objectName)
    {
        EnsureLineRoot();

        GameObject lineObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(WordMatchLineGraphic));
        lineObject.transform.SetParent(lineRoot, false);
        lineObject.transform.SetAsFirstSibling();

        RectTransform rect = lineObject.transform as RectTransform;
        Stretch(rect);

        WordMatchLineGraphic line = lineObject.GetComponent<WordMatchLineGraphic>();
        line.raycastTarget = false;
        line.maskable = false;
        return line;
    }

    private WordMatchEndpoint FindEndpoint(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerCurrentRaycast.gameObject == null)
            return null;

        return eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<WordMatchEndpoint>();
    }

    private Vector2 GetEndpointLocalCenter(WordMatchEndpoint endpoint)
    {
        if (endpoint == null || endpoint.LineAnchor == null)
            return Vector2.zero;

        RectTransform anchor = endpoint.LineAnchor;
        Vector3 world = anchor.TransformPoint(anchor.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineRoot,
            RectTransformUtility.WorldToScreenPoint(EventCamera, world),
            EventCamera,
            out Vector2 local);

        return local;
    }

    private Vector2 ScreenToLineLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineRoot, screenPoint, EventCamera, out Vector2 local);
        return local;
    }

    private Camera EventCamera
    {
        get
        {
            if (canvas == null)
                return null;

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }

    private void AutoWire()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        EnsureLineRoot();
        CollectEndpointsIfNeeded();

        foreach (WordMatchEndpoint endpoint in endpoints)
        {
            if (endpoint != null)
                endpoint.SetController(this);
        }

        if (letterBank == null)
            letterBank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);
    }

    private void UnlockInitiallyKnownLetters()
    {
        string letters = string.IsNullOrWhiteSpace(initiallyKnownLetters)
            ? "CODE"
            : initiallyKnownLetters;

        if (string.IsNullOrWhiteSpace(letters) || enforcingInitialLetters)
            return;

        if (letterBank == null)
            letterBank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);

        if (letterBank == null)
            return;

        enforcingInitialLetters = true;
        letterBank.UnlockLettersInstant(letters);
        enforcingInitialLetters = false;
    }

    private void SubscribeInitialLetterGuards()
    {
        if (letterBank == null)
            letterBank = FindFirstObjectByType<LetterBankController>(FindObjectsInactive.Include);

        if (letterBank != null)
        {
            letterBank.Changed -= HandleLetterBankChanged;
            letterBank.Changed += HandleLetterBankChanged;
        }

        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
    }

    private void UnsubscribeInitialLetterGuards()
    {
        if (letterBank != null)
            letterBank.Changed -= HandleLetterBankChanged;

        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
    }

    private void HandleLetterBankChanged()
    {
        UnlockInitiallyKnownLetters();
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        AutoWire();
        SubscribeInitialLetterGuards();
        UnlockInitiallyKnownLetters();
    }

    private void EnsureLineRoot()
    {
        if (lineRoot != null)
            return;

        RectTransform selfRect = transform as RectTransform;
        if (selfRect == null)
            return;

        Transform existing = transform.Find("__WordMatchLines");
        GameObject rootObject = existing != null
            ? existing.gameObject
            : new GameObject("__WordMatchLines", typeof(RectTransform));

        if (existing == null)
            rootObject.transform.SetParent(transform, false);

        lineRoot = rootObject.transform as RectTransform;
        Stretch(lineRoot);
        lineRoot.SetAsLastSibling();
    }

    private void CollectEndpointsIfNeeded()
    {
        if (!autoCollectEndpoints)
            return;

        bool hasEndpoint = false;
        foreach (WordMatchEndpoint endpoint in endpoints)
        {
            if (endpoint != null)
            {
                hasEndpoint = true;
                break;
            }
        }

        if (!hasEndpoint)
            endpoints = GetComponentsInChildren<WordMatchEndpoint>(true);
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
