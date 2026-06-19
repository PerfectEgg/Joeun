using UnityEngine;

/// <summary>
/// Swaps joint visuals when a specific connector pair becomes linked.
/// This keeps the existing connect-puzzle snap logic untouched.
/// </summary>
public class ConnectJointVisualSwap : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] ConnectorPoint connector;
    [SerializeField] ConnectorPoint expectedLinkedTo;
    bool requireExpectedLinkedTo = true;

    [Header("Visuals")]
    [SerializeField] GameObject[] showWhenDisconnected;
    [SerializeField] GameObject[] showWhenConnected;
    [SerializeField] GameObject[] showWhenWrongConnected;

    bool initializeOnEnable = true;

    enum VisualState
    {
        Disconnected,
        CorrectConnected,
        WrongConnected
    }

    VisualState lastState;
    bool hasState;

    void OnEnable()
    {
        if (!initializeOnEnable) return;

        hasState = false;
        Refresh(true);
    }

    void Update()
    {
        Refresh(false);
    }

    public void RefreshNow()
    {
        Refresh(true);
    }

    void Refresh(bool force)
    {
        VisualState state = GetState();
        if (!force && hasState && state == lastState) return;

        bool correct = state == VisualState.CorrectConnected;
        bool wrong = state == VisualState.WrongConnected;
        bool hasWrongVisual = showWhenWrongConnected != null && showWhenWrongConnected.Length > 0;

        SetObjects(showWhenDisconnected, !correct && (!wrong || !hasWrongVisual));
        SetObjects(showWhenConnected, correct);
        SetObjects(showWhenWrongConnected, wrong);

        lastState = state;
        hasState = true;
    }

    VisualState GetState()
    {
        if (connector == null || !connector.IsLinked)
            return VisualState.Disconnected;

        if (expectedLinkedTo == null)
            return requireExpectedLinkedTo ? VisualState.WrongConnected : VisualState.CorrectConnected;

        bool correct = connector.linkedTo == expectedLinkedTo || expectedLinkedTo.linkedTo == connector;
        return correct ? VisualState.CorrectConnected : VisualState.WrongConnected;
    }

    static void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }
    }
}
