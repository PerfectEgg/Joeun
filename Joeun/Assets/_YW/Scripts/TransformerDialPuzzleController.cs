using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TransformerDialPuzzleController : MonoBehaviour
{
    [SerializeField] private TransformerDialControl2D[] dials;
    [SerializeField] private int[] answerValues;
    [SerializeField] private bool checkOnValueChanged = true;
    [SerializeField] private bool solveOnlyOnce = true;
    [SerializeField, Min(0f)] private float tolerance = 0.01f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved;
    [SerializeField] private UnityEvent onFailed;

    private bool solved;

    public bool IsSolved => solved;

    private void Reset()
    {
        dials = GetComponentsInChildren<TransformerDialControl2D>(true);
    }

    private void OnEnable()
    {
        Subscribe();

        if (checkOnValueChanged)
            CheckSolved(false);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Submit()
    {
        CheckSolved(true);
    }

    public void ResetSolved()
    {
        solved = false;
    }

    public void RefreshDialsFromChildren()
    {
        Unsubscribe();
        dials = GetComponentsInChildren<TransformerDialControl2D>(true);
        Subscribe();
    }

    private void Subscribe()
    {
        if (dials == null)
            return;

        foreach (TransformerDialControl2D dial in dials)
        {
            if (dial != null)
                dial.ValueChanged += HandleDialValueChanged;
        }
    }

    private void Unsubscribe()
    {
        if (dials == null)
            return;

        foreach (TransformerDialControl2D dial in dials)
        {
            if (dial != null)
                dial.ValueChanged -= HandleDialValueChanged;
        }
    }

    private void HandleDialValueChanged(TransformerDialControl2D dial, float value)
    {
        if (checkOnValueChanged)
            CheckSolved(false);
    }

    private void CheckSolved(bool invokeFailed)
    {
        if (solved && solveOnlyOnce)
            return;

        if (!HasValidSetup())
        {
            if (invokeFailed)
                onFailed?.Invoke();

            return;
        }

        for (int i = 0; i < answerValues.Length; i++)
        {
            if (dials[i] == null)
            {
                if (invokeFailed)
                    onFailed?.Invoke();

                return;
            }

            if (Mathf.Abs(dials[i].Value - answerValues[i]) > tolerance)
            {
                if (invokeFailed)
                    onFailed?.Invoke();

                return;
            }
        }

        solved = true;
        onSolved?.Invoke();
    }

    private bool HasValidSetup()
    {
        return dials != null
            && answerValues != null
            && dials.Length > 0
            && dials.Length == answerValues.Length;
    }
}
