using System;

public static class SkillInteractionLock
{
    static int lockCount;

    public static event Action<bool> OnChanged;
    public static bool IsLocked => lockCount > 0;

    public static void Push()
    {
        bool wasLocked = IsLocked;
        lockCount++;

        if (!wasLocked)
            OnChanged?.Invoke(true);
    }

    public static void Pop()
    {
        if (lockCount <= 0)
            return;

        bool wasLocked = IsLocked;
        lockCount--;

        if (wasLocked && !IsLocked)
            OnChanged?.Invoke(false);
    }

    public static void Clear()
    {
        if (!IsLocked)
            return;

        lockCount = 0;
        OnChanged?.Invoke(false);
    }
}
