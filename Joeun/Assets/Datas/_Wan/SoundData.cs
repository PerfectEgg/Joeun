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

[CreateAssetMenu(fileName = "StageSound", menuName = "Game Data/Stage Sound")]
public class StageSound : ScriptableObject
{
    [Header("해당 스테이지 전용 효과음 목록")]
    public List<SoundData> sfxList = new List<SoundData>();

    /// <summary>
    /// ID로 오디오 클립을 찾아 반환합니다.
    /// </summary>
    public SoundData GetSoundData(string id)
    {
        return sfxList.Find(x => x.soundID == id);
    }
}