using UnityEngine;
using System.Collections;
using System;

// ==========================================
// 탈출구 문 클래스
// 설명: 1스테이지 문을 제외한 탈출구 문에 대한 클래스입니다.
// ==========================================
public class ExitDoor : MonoBehaviour
{
    private float _moveSpeed = 4f;
    [SerializeField] private float _xSize = 6.2f;
    [SerializeField] private Transform _originLeftTransform;
    [SerializeField] private Transform _originRightTransform;
    private Vector3 _originLeftPosition;
    private Vector3 _originRightPosition;
    private Vector3 _targetLeftPosition;
    private Vector3 _targetRightPosition;
    

    // 문이 이미 이동 중인지 체크하여 중복 실행을 방지하는 플래그
    private bool _isMoving = false;

    void Start()
    {
        _originLeftPosition = _originLeftTransform.position;

        _targetLeftPosition = _originLeftPosition;
        _targetLeftPosition.x -= transform.localScale.x * _xSize / 2;

        _originRightPosition =_originRightTransform.position;

        _targetRightPosition = _originRightPosition;
        _targetRightPosition.x += transform.localScale.x * _xSize / 2;
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
        float leftDistance = Vector3.Distance(_originLeftPosition, _targetLeftPosition);
        float rightDistance = Vector3.Distance(_originRightPosition, _targetRightPosition);
        
        float distance = Math.Max(leftDistance, rightDistance);

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
            _originLeftTransform.position = Vector3.Lerp(_originLeftPosition, _targetLeftPosition, smoothT);
            _originRightTransform.position = Vector3.Lerp(_originRightPosition, _targetRightPosition, smoothT);

            // 다음 프레임까지 대기
            yield return null;
        }

        // 오차를 없애기 위해 마지막에 정확히 타겟 위치로 고정
        _originLeftTransform.position = _targetLeftPosition;
        _originRightTransform.position = _targetRightPosition;
    }
}
