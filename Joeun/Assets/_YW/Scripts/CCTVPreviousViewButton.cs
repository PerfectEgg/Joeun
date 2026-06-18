using UnityEngine;

public class CCTVPreviousViewButton : MonoBehaviour
{
    private CCTVManager cachedManager;

    public void GoBack()
    {
        if (cachedManager == null || !cachedManager.isActiveAndEnabled)
            cachedManager = FindFirstObjectByType<CCTVManager>();

        if (cachedManager == null)
            return;

        cachedManager.GoToPreviousView();
    }
}
