using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum WordMatchEndpointSide
{
    Source,
    Target
}

[DisallowMultipleComponent]
public sealed class WordMatchEndpoint : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private WordMatchPuzzleController controller;
    [SerializeField] private WordMatchEndpointSide side;
    [SerializeField] private string matchKey;
    [SerializeField] private string rewardLetters;
    [SerializeField] private Graphic portGraphic;
    [SerializeField] private RectTransform lineAnchor;
    [SerializeField] private GameObject[] objectsToShowOnMatch = Array.Empty<GameObject>();
    [SerializeField] private RecognitionDecodeAreaController[] decodeAreasToReady = Array.Empty<RecognitionDecodeAreaController>();
    [SerializeField] private bool hideDecodeObjectsUntilMatched = true;
    [SerializeField, HideInInspector] private bool createSelfDecodeRewardWhenNoTargets;
    [SerializeField] private bool suggested;

    private Color baseColor = Color.white;
    private bool hasBaseColor;
    private bool matched;
    private bool hovering;

    public WordMatchEndpointSide Side => side;
    public string MatchKey => NormalizeKey(matchKey);
    public string RewardLetters => rewardLetters;
    public bool IsSuggested => suggested;
    public bool IsMatched => matched;
    public RectTransform RectTransform => transform as RectTransform;
    public RectTransform LineAnchor => lineAnchor != null ? lineAnchor : RectTransform;

    private void Awake()
    {
        AutoWire();
        CaptureBaseColor();
        HidePendingDecodeObjects();
        ApplyState();
    }

    private void Reset()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        HidePendingDecodeObjects();
        ApplyState();
    }

    private void Update()
    {
        if (!suggested || matched || portGraphic == null)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * 3.6f) + 1f) * 0.5f;
        Color color = Color.Lerp(baseColor, new Color(0.82f, 1f, 0.72f, 1f), pulse * 0.42f);
        color.a = Mathf.Lerp(0.72f, 1f, pulse);
        portGraphic.color = color;
    }

    public void SetController(WordMatchPuzzleController owner)
    {
        controller = owner;
    }

    public void SetMatched(bool value)
    {
        matched = value;
        if (!matched)
            HidePendingDecodeObjects();

        ApplyState();
    }

    public void ResetMatchState()
    {
        matched = false;
        HidePendingDecodeObjects();
        ApplyState();
    }

    public bool ActivateDecodeRewards()
    {
        bool activated = false;

        foreach (GameObject target in objectsToShowOnMatch)
        {
            if (target == null)
                continue;

            target.SetActive(true);
            activated = true;
        }

        foreach (RecognitionDecodeAreaController area in decodeAreasToReady)
        {
            if (area == null)
                continue;

            if (!area.gameObject.activeSelf)
                area.gameObject.SetActive(true);

            area.MarkDecodeReady();
            activated = true;
        }

        if (!activated)
            activated = ActivateChildLetterDecodeRewards();

        if (!activated && createSelfDecodeRewardWhenNoTargets)
            activated = ActivateSelfDecodeReward();

        return activated;
    }

    public void PlayInvalidFeedback()
    {
        if (portGraphic == null)
            return;

        StopAllCoroutines();
        StartCoroutine(InvalidFeedbackRoutine());
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.EndDrag(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ApplyState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ApplyState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null)
            controller.HandleEndpointClick(this, eventData);
    }

    private void AutoWire()
    {
        if (controller == null)
            controller = GetComponentInParent<WordMatchPuzzleController>();

        if (portGraphic == null)
            portGraphic = GetComponent<Graphic>();

        if (lineAnchor == null && portGraphic != null)
            lineAnchor = portGraphic.rectTransform;
    }

    private void HidePendingDecodeObjects()
    {
        if (!hideDecodeObjectsUntilMatched || matched)
            return;

        foreach (GameObject target in objectsToShowOnMatch)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    private bool ActivateSelfDecodeReward()
    {
        if (string.IsNullOrWhiteSpace(rewardLetters))
            return false;

        RecognitionDecodeAreaController area = GetComponent<RecognitionDecodeAreaController>();
        if (area == null)
            area = gameObject.AddComponent<RecognitionDecodeAreaController>();

        area.SetExpectedText(rewardLetters);
        area.MarkDecodeReady();

        LetterBankUnlockReward reward = GetComponent<LetterBankUnlockReward>();
        if (reward == null)
            reward = gameObject.AddComponent<LetterBankUnlockReward>();

        reward.SetLettersToUnlock(rewardLetters);
        return true;
    }

    private bool ActivateChildLetterDecodeRewards()
    {
        if (string.IsNullOrWhiteSpace(rewardLetters))
            return false;

        bool activated = false;
        string normalizedRewards = rewardLetters.Trim().ToUpperInvariant();
        LetterBankLetterView[] letterViews = GetComponentsInChildren<LetterBankLetterView>(true);

        foreach (LetterBankLetterView letterView in letterViews)
        {
            if (letterView == null || !ContainsLetter(normalizedRewards, letterView.Letter))
                continue;

            activated |= letterView.TryMarkDecodeReady();
        }

        return activated;
    }

    private void CaptureBaseColor()
    {
        if (hasBaseColor || portGraphic == null)
            return;

        baseColor = portGraphic.color;
        hasBaseColor = true;
    }

    private void ApplyState()
    {
        AutoWire();
        CaptureBaseColor();

        if (portGraphic == null || suggested && !matched)
            return;

        Color color = baseColor;

        if (matched)
            color = new Color(0.66f, 1f, 0.56f, 1f);
        else if (hovering)
            color = Color.Lerp(baseColor, Color.white, 0.34f);

        portGraphic.color = color;
    }

    private System.Collections.IEnumerator InvalidFeedbackRoutine()
    {
        Color invalid = new Color(1f, 0.22f, 0.22f, 1f);

        for (int i = 0; i < 2; i++)
        {
            portGraphic.color = invalid;
            yield return new WaitForSecondsRealtime(0.055f);
            ApplyState();
            yield return new WaitForSecondsRealtime(0.055f);
        }
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static bool ContainsLetter(string letters, char value)
    {
        if (!char.IsLetter(value) || string.IsNullOrWhiteSpace(letters))
            return false;

        char normalized = char.ToUpperInvariant(value);
        foreach (char letter in letters)
        {
            if (char.ToUpperInvariant(letter) == normalized)
                return true;
        }

        return false;
    }
}
