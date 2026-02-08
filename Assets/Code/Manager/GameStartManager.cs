using System.Collections;
using UnityEngine;

public class GameStartManager : MonoBehaviour
{

    private IEnumerator Start()
    {
        // 1) DB 로드
        GameSaveLoadManager.Instance.LoadGame();

        yield return null; // 안전하게 1프레임 대기
    }
}
