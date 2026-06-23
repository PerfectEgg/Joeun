using System;

public enum HandPuzzleDirection
{
    Up = 0,
    UpRight = 1,
    DownRight = 2,
    Down = 3,
    DownLeft = 4,
    UpLeft = 5
}

[Flags]
public enum HandPuzzleDirectionMask
{
    None = 0,
    Up = 1 << 0,
    UpRight = 1 << 1,
    DownRight = 1 << 2,
    Down = 1 << 3,
    DownLeft = 1 << 4,
    UpLeft = 1 << 5,
    All = Up | UpRight | DownRight | Down | DownLeft | UpLeft
}

public static class HandPuzzleDirectionUtility
{
    public static HandPuzzleDirection Opposite(HandPuzzleDirection direction)
    {
        return (HandPuzzleDirection)(((int)direction + 3) % 6);
    }

    public static bool Contains(this HandPuzzleDirectionMask mask, HandPuzzleDirection direction)
    {
        return (mask & ToMask(direction)) != 0;
    }

    public static bool ContainsAll(this HandPuzzleDirectionMask mask, HandPuzzleDirectionMask required)
    {
        return required != HandPuzzleDirectionMask.None && (mask & required) == required;
    }

    public static HandPuzzleDirectionMask ToMask(HandPuzzleDirection direction)
    {
        return (HandPuzzleDirectionMask)(1 << (int)direction);
    }
}
