using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Switches a multi-piece assembly to a completed visual state.
/// Intended to be called from ConnectPuzzleManager.onPuzzleSolved.
/// </summary>
public class ShowCompletedAssembly : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float switchDelay = 0.18f;
    [SerializeField] private float showFadeDuration = 0.18f;

    [Header("Hide Visuals")]
    [SerializeField] private Image[] hideImages;

    [Header("Disable Components")]
    [SerializeField] private Behaviour[] disableBehaviours;

    [Header("Hide Objects")]
    [SerializeField] private GameObject[] hideObjects;

    [Header("Show Objects")]
    [SerializeField] private GameObject[] showObjects;

    private Coroutine showRoutine;

    public void Show()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void ShowImmediate()
    {
        SetImagesEnabled(hideImages, false);
        SetBehavioursEnabled(disableBehaviours, false);
        SetObjectsActive(hideObjects, false);
        SetObjectsActive(showObjects, true);
        SetShowObjectsAlpha(1f);
    }

    public void ResetState()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        SetImagesEnabled(hideImages, true);
        SetBehavioursEnabled(disableBehaviours, true);
        SetObjectsActive(hideObjects, true);
        SetObjectsActive(showObjects, false);
        SetShowObjectsAlpha(1f);
    }

    private IEnumerator ShowRoutine()
    {
        if (switchDelay > 0f)
            yield return new WaitForSeconds(switchDelay);

        SetShowObjectsAlpha(0f);
        SetObjectsActive(showObjects, true);

        if (showFadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < showFadeDuration)
            {
                elapsed += Time.deltaTime;
                SetShowObjectsAlpha(Mathf.Clamp01(elapsed / showFadeDuration));
                yield return null;
            }
        }

        SetShowObjectsAlpha(1f);
        SetImagesEnabled(hideImages, false);
        SetBehavioursEnabled(disableBehaviours, false);
        SetObjectsActive(hideObjects, false);
        showRoutine = null;
    }

    private static void SetImagesEnabled(Image[] images, bool enabled)
    {
        if (images == null)
            return;

        foreach (Image image in images)
        {
            if (image != null)
                image.enabled = enabled;
        }
    }

    private static void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void SetShowObjectsAlpha(float alpha)
    {
        if (showObjects == null)
            return;

        foreach (GameObject obj in showObjects)
        {
            if (obj == null)
                continue;

            CanvasGroup group = obj.GetComponent<CanvasGroup>();
            if (group == null)
                group = obj.AddComponent<CanvasGroup>();

            group.alpha = alpha;
        }
    }
}
