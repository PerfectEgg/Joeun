using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Places a completed assembly visual by matching two anchors on it to
/// reference anchors on the assembled puzzle parts.
/// </summary>
[DisallowMultipleComponent]
public class CompletedAssemblyAnchorAligner : MonoBehaviour
{
    private const float MinAnchorDistance = 0.001f;

    [Header("Complete Visual")]
    [SerializeField] private RectTransform completeRoot;
    [SerializeField] private RectTransform completeAnchor1;
    [SerializeField] private RectTransform completeAnchor2;

    [Header("Source Anchors")]
    [FormerlySerializedAs("arm1Reference")]
    [SerializeField] private RectTransform sourceAnchor1;
    [FormerlySerializedAs("arm4Reference")]
    [SerializeField] private RectTransform sourceAnchor2;

    [Header("Options")]
    [SerializeField] private bool matchScale = true;

    private bool loggedSetupWarning;

    private void Reset()
    {
        completeRoot = transform as RectTransform;
    }

    private void OnValidate()
    {
        if (completeRoot == null)
            completeRoot = transform as RectTransform;
    }

    public bool ApplyAlignment()
    {
        if (completeRoot == null)
            completeRoot = transform as RectTransform;

        if (completeRoot == null || completeAnchor1 == null || completeAnchor2 == null)
        {
            WarnSetup("Complete Root, Complete Anchor 1, and Complete Anchor 2 must be assigned.");
            return false;
        }

        if (sourceAnchor1 == null || sourceAnchor2 == null || sourceAnchor1 == sourceAnchor2)
        {
            WarnSetup("Two different source anchors must be assigned.");
            return false;
        }

        return AlignTo(sourceAnchor1, sourceAnchor2);
    }

    private bool AlignTo(RectTransform sourceAnchor1, RectTransform sourceAnchor2)
    {
        Vector2 sourceVector = ToVector2(sourceAnchor2.position - sourceAnchor1.position);
        Vector2 completeVector = ToVector2(completeAnchor2.position - completeAnchor1.position);

        float sourceDistance = sourceVector.magnitude;
        float completeDistance = completeVector.magnitude;
        if (sourceDistance <= MinAnchorDistance || completeDistance <= MinAnchorDistance)
        {
            WarnSetup("Anchor distance is too small to align.");
            return false;
        }

        if (matchScale)
        {
            float scaleFactor = sourceDistance / completeDistance;
            Vector3 localScale = completeRoot.localScale;
            completeRoot.localScale = new Vector3(
                localScale.x * scaleFactor,
                localScale.y * scaleFactor,
                localScale.z);

            completeVector = ToVector2(completeAnchor2.position - completeAnchor1.position);
        }

        float angle = Vector2.SignedAngle(completeVector, sourceVector);
        completeRoot.rotation = Quaternion.AngleAxis(angle, Vector3.forward) * completeRoot.rotation;

        Vector3 positionOffset = sourceAnchor1.position - completeAnchor1.position;
        positionOffset.z = 0f;
        completeRoot.position += positionOffset;
        return true;
    }

    private static Vector2 ToVector2(Vector3 value)
    {
        return new Vector2(value.x, value.y);
    }

    private void WarnSetup(string message)
    {
        if (loggedSetupWarning)
            return;

        Debug.LogWarning($"{nameof(CompletedAssemblyAnchorAligner)}: {message}", this);
        loggedSetupWarning = true;
    }
}
