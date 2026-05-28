using UnityEngine;

// 상호작용 인터페이스
public interface IInteractive
{
    void Interact();
}

// 열고 닫기 가능한 인터페이스 [상호작용 인터페이스 상속]
public interface IOpenable : IInteractive
{
    bool IsOpen { get; }
    void Open();
    void Close();
}

// 잠긴 인터페이스 [상호작용 인터페이스 상속] (열쇠 등으로 열어야 함)
public interface IPickable : IInteractive
{
    bool IsLocked { get; }

    // 특정 아이템 ID나 태그를 넘겨 잠금 해제 시도
    bool TryUnlock(string keyId);
}

// 파괴 가능한 인터페이스 [상호작용 인터페이스 상속]
public interface IDestructible : IInteractive
{
    void DestroyObject();
}

// 드래그 가능한 인터페이스 [상호작용 인터페이스 상속]
public interface IDraggable : IInteractive
{
    Vector2 OriginPosition { get; }
    bool IsAcquired { get; }

    void OnAcquire();
    void OnDragStart();
    void OnDrag(Vector2 currentMousePosition);
    void OnDragEnd();
}

// 퍼즐 인터페이스
public interface IPuzzleable
{
    bool IsSolved { get; }
    void StartPuzzle();
    void CompletePuzzle();
}

// 마우스 오버 반응 인터페이스
public interface IHoverable
{
    void OnHoverEnter();
    void OnHoverExit();
}
