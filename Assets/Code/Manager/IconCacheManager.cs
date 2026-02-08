using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class IconCacheManager : MonoBehaviour
{
    public static IconCacheManager Instance { get; private set; }

    private readonly Dictionary<string, Sprite> _iconCache = new();
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handles = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 아이콘 가져오기 (없으면 Addressables 로드 후 캐싱)
    /// </summary>
    public async Task<Sprite> GetIconAsync(string addrKey)
    {
        // 1) 이미 캐시에 있으면 바로 반환
        if (_iconCache.TryGetValue(addrKey, out var cached))
            return cached;

        // 2) 없으면 Addressables 로드
        var handle = Addressables.LoadAssetAsync<Sprite>(addrKey);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[IconCache] 아이콘 로드 실패: {addrKey}");
            return null;
        }

        Sprite sprite = handle.Result;

        // 3) 캐시에 저장 + 핸들도 저장 (나중에 Release용)
        _iconCache[addrKey] = sprite;
        _handles[addrKey] = handle;

        return sprite;
    }

    /// <summary>
    /// 특정 아이콘만 메모리에서 제거하고 싶을 때
    /// </summary>
    public void ReleaseIcon(string addrKey)
    {
        if (_handles.TryGetValue(addrKey, out var handle))
        {
            Addressables.Release(handle);
            _handles.Remove(addrKey);
        }

        _iconCache.Remove(addrKey);
    }

    /// <summary>
    /// 전부 다 날리기 (씬 전환 시 등)
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in _handles)
        {
            Addressables.Release(kv.Value);
        }
        _handles.Clear();
        _iconCache.Clear();
    }
}
