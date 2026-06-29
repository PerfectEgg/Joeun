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
        private bool lastIsOn;

        public void Apply(bool force, bool syncTargetRotation)
        {
            if (valve == null)
                return;

            bool isOn = valve.IsOn;
            if (!force && hasState && lastIsOn == isOn)
            {
                ApplyRotation(syncTargetRotation);
                return;
            }

            hasState = true;
            lastIsOn = isOn;

            SetActive(offObject, !isOn);
            SetActive(onObject, isOn);
            ApplyRotation(syncTargetRotation);
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
