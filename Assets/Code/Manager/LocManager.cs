using System.Collections.Generic;
using UnityEngine;

public class LocManager : MonoBehaviour
{
    public static LocManager Instance { get; private set; }

    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Korean;

    public event System.Action OnLanguageChanged;

    private Dictionary<string, C_LOC_Texts> _dict;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllTextsIntoMemory();

        // 처음 시작할 때 시스템 언어로 세팅 (선택)
        SetLanguageFromSystem();
    }

    private void LoadAllTextsIntoMemory()
    {
        _dict = new Dictionary<string, C_LOC_Texts>(C_LOC_Texts.CountEntities);

        C_LOC_Texts.ForEachEntity(row =>
        {
            if (!_dict.ContainsKey(row.F_key))
                _dict.Add(row.F_key, row);
        });
    }

    public void SetLanguage(GameLanguage lang)
    {
        if (CurrentLanguage == lang) return;

        CurrentLanguage = lang;

        // TODO: PlayerPrefs 등에 저장해서 다음 실행 시 유지해도 됨

        // 언어 바뀌었다고 알림
        OnLanguageChanged?.Invoke();
    }

    private void SetLanguageFromSystem()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean: CurrentLanguage = GameLanguage.Korean; break;
            case SystemLanguage.Japanese: CurrentLanguage = GameLanguage.Japanese; break;
            default: CurrentLanguage = GameLanguage.English; break;
        }
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!_dict.TryGetValue(key, out var row))
            return key;

        return CurrentLanguage switch
        {
            GameLanguage.Korean => row.F_ko,
            GameLanguage.English => row.F_en,
            GameLanguage.Japanese => row.F_jp,
            _ => row.F_en
        };
    }
}
