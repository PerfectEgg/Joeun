using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 스테이지 사운드 클래스
// 설명: 스테이지 별로 사운드를 저장합니다.
// ==========================================
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