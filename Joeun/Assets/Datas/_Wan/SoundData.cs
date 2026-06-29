using UnityEngine;
using System.Collections.Generic;
using System;

// ==========================================
// 사운드 데이터 클래스
// 설명: 사운드의 기본 정보를 저장합니다. (사운드 하나의 정보 [ID와 실제 파일 매칭])
// ==========================================
[Serializable]
public class SoundData
{
    public string soundID;       // 예: "DoorOpen", "DecodeSuccess"
    public AudioClip clip;       // 실제 오디오 파일
    [Range(0f, 1f)] 
    public float volume = 1.0f;  // 사운드별 개별 볼륨 조절용
}