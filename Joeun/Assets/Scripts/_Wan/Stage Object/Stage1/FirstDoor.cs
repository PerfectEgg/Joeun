using UnityEngine;
using System.Collections;

// ==========================================
// 첫번째 문 클래스
// 설명: 1스테이지 오브젝트인 첫번째 문에 대한 클래스입니다.
// ==========================================
public class FirstDoor : MonoBehaviour
{
    private float _moveSpeed = 7.5f;
    [SerializeField] private float _xSize = 5.4f;
    private Vector3 _originPosition;
    private Vector3 _targetPosition;

    // 문이 이미 이동 중인지 체크하여 중복 실행을 방지하는 플래그
    private bool _isMoving = false;

    void Start()
    {
        _originPosition = transform.position;
        _targetPosition = _originPosition;
        _targetPosition.x += transform.localScale.x * _xSize;
    }

    public void MoveDoor()
    {
        if (!_isMoving)
        {
            DevLog.Log("실행");
            StartCoroutine(MoveDoorCoroutine());
        }
    }

    // 실제 문을 부드럽게 이동시키는 코루틴
    private IEnumerator MoveDoorCoroutine()
    {
        _isMoving = true;
        float elapsedTime = 0f;
        
        // 이동해야 할 총 거리 계산
        float distance = Vector3.Distance(_originPosition, _targetPosition);
        
        // 속도(Speed)를 기반으로 총 소요 시간(Duration) 계산 (시간 = 거리 / 속력)
        float duration = distance / _moveSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 0부터 1까지 선형적으로 증가하는 진행률 (Linear t)
            float t = elapsedTime / duration;
            
            // [핵심] SmoothStep을 적용하여 시작과 끝은 느리게, 중간은 빠르게 곡선 처리
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Lerp(선형 보간)에 부드러운 진행률(smoothT)을 넣어 위치 적용
            transform.position = Vector3.Lerp(_originPosition, _targetPosition, smoothT);

            // 다음 프레임까지 대기
            yield return null;
        }

        // 오차를 없애기 위해 마지막에 정확히 타겟 위치로 고정
        transform.position = _targetPosition;
    }
}
