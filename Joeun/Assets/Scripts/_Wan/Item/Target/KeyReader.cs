using UnityEngine;

// ==========================================
// 키 인식 오브젝트 클래스
// ==========================================
public class KeyReader : MonoBehaviour, IPickable
{
    [Header("잠금 설정")]
    [SerializeField] private string _requiredKeyID; // 예: "CardKey_Lv1"
    
    [Header("작동할 대상")]
    [Tooltip("이 장치가 해금되었을 때 열릴 문을 연결하세요.")]
    [SerializeField] private Door _targetDoor;

    [Header("Z 레이어 설정")]
    [Tooltip("열쇠의 물리 충돌을 위해 Z 레이어를 낮춰서 화면 앞으로 나타내는 설정값입니다.")]
    [SerializeField] private int _setZLayer = 1; // Z레이어를 낮춰서 플레이어 뒤에 숨기기 위한 설정값

    public bool IsLocked { get; private set; } = true;

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
    }

    #region IPickable 구현
    public bool TryUnlock(string keyId)
    {
        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 장치입니다.");
            return false;
        }

        if (keyId == _requiredKeyID)
        {
            IsLocked = false;
            DevLog.Log("삐빅- 인증되었습니다.");

            // 중앙 버스에 문이 열렸음을 방송 (업적, 효과음, 퍼즐 연동용)
            GameEvent.EOnLockOpened?.Invoke(gameObject.name);
            
            // 핵심: 내 판정이 성공했으니, 연결된 문에게 잠금을 풀라고 명령!
            _targetDoor.UnlockFromExternal();
            
            // 장치 자체의 시각적 변화 (예: 빨간불 -> 초록불)
            // GetComponent<SpriteRenderer>().sprite = unlockedSprite;
            
            return true; // 성공했으니 아이템(카드키) 소모
        }
        else
        {
            DevLog.Log("접근 권한이 없는 카드키입니다.");
            return false; // 실패
        }
    }
    #endregion
}