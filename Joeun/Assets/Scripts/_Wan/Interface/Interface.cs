using UnityEngine;

// ==========================================
// 인터페이스 관련 코드 모음
// ==========================================

// 상호작용 인터페이스
public interface IInteractive
{
    void Interact();
}

// 열고 닫기 가능한 인터페이스
public interface IOpenable
{
    bool IsLocked { get; }
    bool IsRecyclable { get; }
    bool IsOpen { get; }
    void Open();
    void Close();
}

// 획득 가능한 인터페이스
public interface ICollectible
{
    bool IsAcquired { get; }

    void Collect();
    void OnAcquire();
}

// 잠긴 인터페이스 (열쇠 등으로 열어야 함)
public interface IPickable
{
    bool IsLocked { get; }

    // 특정 아이템 ID나 태그를 넘겨 잠금 해제 시도
    bool TryUnlock(string keyId);
}

// 파괴 가능한 인터페이스
public interface IDestructible
{
    void DestroyObject();
}

// 드래그 가능한 인터페이스
public interface IDraggable
{
    Vector2 OriginPosition { get; }
    
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