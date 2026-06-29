using UnityEngine;
using UnityEngine.Events;

public sealed class ProgressiveWindowMover : MonoBehaviour, IConditionRequirable
{
    [SerializeField] Transform window;
    [SerializeField] Transform openTarget;
    [SerializeField, Min(1)] int stepCount = 3;
    [SerializeField] UnityEvent onCompleted;

    Vector3 closedLocalPosition;
    int completedStepCount;
    bool isCompleted;
    bool initialized;
    bool switchboardAdvanced;
    bool coolantAdvanced;
    bool transformerAdvanced;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (window == null)
            window = transform;

        if (initialized)
            return;

        closedLocalPosition = window.localPosition;
        initialized = true;
        ApplyProgressImmediate();
    }

    public void ResolveCondition()
    {
        Initialize();
        AdvanceStep();
    }

    public void AdvanceStep()
    {
        Initialize();
        SetCompletedStepCount(completedStepCount + 1);
    }

    public void SetCompletedStepCount(int count)
    {
        Initialize();
        completedStepCount = Mathf.Clamp(count, 0, stepCount);

        if (completedStepCount < stepCount)
            isCompleted = false;

        float progress = (float)completedStepCount / stepCount;
        ApplyProgressImmediate();

        if (completedStepCount >= stepCount)
            CompleteOnce();
    }

    public void ResetProgress()
    {
        Initialize();
        switchboardAdvanced = false;
        coolantAdvanced = false;
        transformerAdvanced = false;
        SetCompletedStepCount(0);
    }

    public void AdvanceSwitchboard()
    {
        Initialize();
        if (switchboardAdvanced)
            return;

        switchboardAdvanced = true;
        AdvanceStep();
    }

    public void AdvanceCoolant()
    {
        Initialize();
        if (coolantAdvanced)
            return;

        coolantAdvanced = true;
        AdvanceStep();
    }

    public void AdvanceTransformer()
    {
        Initialize();
        if (transformerAdvanced)
            return;

        transformerAdvanced = true;
        AdvanceStep();
    }

    public void OpenImmediate()
    {
        Initialize();
        completedStepCount = stepCount;
        ApplyProgressImmediate();
        CompleteOnce();
    }

    void ApplyProgressImmediate()
    {
        if (window == null)
            return;

        float progress = (float)completedStepCount / stepCount;
        window.localPosition = Vector3.Lerp(closedLocalPosition, GetOpenLocalPosition(), progress);
    }

    Vector3 GetOpenLocalPosition()
    {
        if (openTarget == null)
            return closedLocalPosition;

        if (window != null && window.parent != null)
            return window.parent.InverseTransformPoint(openTarget.position);

        return openTarget.localPosition;
    }

    void CompleteOnce()
    {
        if (isCompleted)
            return;

        isCompleted = true;
        onCompleted?.Invoke();
    }
}
