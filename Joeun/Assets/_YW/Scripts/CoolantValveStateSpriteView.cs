using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoolantValveStateSpriteView : MonoBehaviour
{
    [SerializeField] private bool syncTargetRotation = true;
    [SerializeField] private List<Entry> entries = new List<Entry>();

    [System.Serializable]
    private sealed class Entry
    {
        [SerializeField] private string label;
        [SerializeField] private CoolantValve valve;
        [SerializeField] private GameObject offObject;
        [SerializeField] private GameObject onObject;

        private bool hasState;
        private bool displayedIsOn;
        private bool hasPendingState;
        private bool pendingIsOn;
        private float switchAtTime;

        public void Apply(bool force, bool syncTargetRotation)
        {
            if (valve == null)
                return;

            bool isOn = valve.IsOn;
            ApplyRotation(syncTargetRotation);

            if (force || !hasState)
            {
                ApplyStateImmediate(isOn);
                return;
            }

            if (hasPendingState)
            {
                if (pendingIsOn != isOn)
                {
                    pendingIsOn = isOn;
                    switchAtTime = Time.time + GetSwitchDelay();
                }

                if (Time.time >= switchAtTime)
                    ApplyStateImmediate(pendingIsOn);

                return;
            }

            if (displayedIsOn == isOn)
                return;

            pendingIsOn = isOn;
            hasPendingState = true;
            switchAtTime = Time.time + GetSwitchDelay();
        }

        private void ApplyStateImmediate(bool isOn)
        {
            hasState = true;
            hasPendingState = false;
            displayedIsOn = isOn;

            SetActive(offObject, !isOn);
            SetActive(onObject, isOn);
        }

        private float GetSwitchDelay()
        {
            return valve != null ? Mathf.Max(0f, valve.rotateAnimDuration) : 0f;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target == null || target.activeSelf == active)
                return;

            target.SetActive(active);
        }

        private void ApplyRotation(bool syncTargetRotation)
        {
            if (!syncTargetRotation)
                return;

            Quaternion rotation = valve.handle != null
                ? valve.handle.localRotation
                : Quaternion.Euler(0f, 0f, -valve.CurrentAngle);

            ApplyRotation(offObject, rotation);
            ApplyRotation(onObject, rotation);
        }

        private static void ApplyRotation(GameObject target, Quaternion rotation)
        {
            if (target == null)
                return;

            target.transform.localRotation = rotation;
        }
    }

    private void Reset()
    {
        AutoCollectEntries();
    }

    private void Awake()
    {
        Apply(true);
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void LateUpdate()
    {
        Apply(false);
    }

    public void Refresh()
    {
        Apply(true);
    }

    private void Apply(bool force)
    {
        foreach (Entry entry in entries)
            entry?.Apply(force, syncTargetRotation);
    }

    private void AutoCollectEntries()
    {
        if (entries == null)
            entries = new List<Entry>();
    }
}
