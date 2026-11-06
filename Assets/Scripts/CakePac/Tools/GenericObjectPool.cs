using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Sirenix.OdinInspector;

/*
    使用示例：
    // 获取粒子实例
    var particle = ObjectPool.Instance.Get(explosionPrefab, position);
    
    // 获取音频实例
    var audio = ObjectPool.Instance.Get(soundPrefab, position);
    
    // 归还对象
    ObjectPool.Instance.Release(particle);
*/

/// <summary>
/// 通用对象池管理器 - 统一管理所有类型的对象池
/// </summary>
public class GenericObjectPool : Singleton<GenericObjectPool>
{
    private const int DEFAULT_CAPACITY = 20;
    private const int DEFAULT_MAX_SIZE = 200;
    private const float DEFAULT_AUTO_RELEASE_TIME = 5f;

    private Dictionary<string, object> _pools = new Dictionary<string, object>();
    private Dictionary<string, Transform> _parentTransforms = new Dictionary<string, Transform>();

    #region 公共API
    /// <summary>
    /// 传入Prefab获取对象池实例，Release时传入的类型需要与Get时一致
    /// </summary>
    /// <param name="prefab"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Get<T>(T prefab) where T : Component
    {
        if (prefab == null) return null;

        string poolName = GetPoolKey<T>(prefab);
        EnsurePoolExists(poolName, prefab);

        var pool = (ObjectPool<T>)_pools[poolName];
        var obj = pool.Get();
        return obj;
    }

    public T Get<T>(T prefab, Vector3 position) where T : Component
    {
        T obj = Get(prefab);
        if (obj != null)
        {
            obj.transform.position = position;
        }
        return obj;
    }

    public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        T obj = Get(prefab, position);
        if (obj != null)
        {
            obj.transform.rotation = rotation;
        }
        return obj;
    }

    /// <summary>
    /// 获取对象,设置位置,并自动回收
    /// </summary>
    /// <param name="autoReleaseTime">自动回收时间，-1表示使用默认时间</param>
    public T GetWithAutoRelease<T>(T prefab, Vector3 position, float autoReleaseTime = -1f) where T : Component
    {
        T obj = Get(prefab, position);
        if (obj != null)
        {
            float releaseTime = autoReleaseTime > 0 ? autoReleaseTime : DEFAULT_AUTO_RELEASE_TIME;
            StartCoroutine(AutoReleaseCoroutine(obj, releaseTime));
        }
        return obj;
    }
    /// <summary>
    /// 获取对象，设置位置及旋转，并自动回收
    /// </summary>
    /// <param name="autoReleaseTime">自动回收时间，-1表示使用默认时间</param>
    public T GetWithAutoRelease<T>(T prefab, Vector3 position, Quaternion rotation, float autoReleaseTime = -1f) where T : Component
    {
        T obj = Get(prefab, position, rotation);
        if (obj != null)
        {
            float releaseTime = autoReleaseTime > 0 ? autoReleaseTime : DEFAULT_AUTO_RELEASE_TIME;
            StartCoroutine(AutoReleaseCoroutine(obj, releaseTime));
        }
        return obj;
    }

    /// <summary>
    /// 归还对象到池中,传入的类型需要与Get时一致。注意传入的是泛型类型(一般是this)，而不一定是GameObject
    /// </summary>
    public void Release<T>(T obj) where T : Component
    {
        if (obj == null) return;

        string poolName = GetPoolKeyForObjName(obj.name);

        if (_pools.ContainsKey(poolName))
        {
            var pool = (ObjectPool<T>)_pools[poolName];
            pool.Release(obj);
        }
        else
        {
            Debug.LogWarning($"ObjectPool: 无法找到对象 {obj.name} 对应的池! 池名: {poolName}");
        }
    }

    public void Clear<T>(T prefab) where T : Component
    {
        if (prefab == null) return;

        string poolName = GetPoolKey<T>(prefab);
        if (_pools.ContainsKey(poolName))
        {
            var pool = (ObjectPool<T>)_pools[poolName];
            pool.Clear();
        }
    }

    public void ClearAll()
    {
        foreach (var kvp in _pools)
        {
            // 使用反射调用Clear方法，因为_pools存储的是object类型
            var poolType = kvp.Value.GetType();
            var clearMethod = poolType.GetMethod("Clear");
            clearMethod?.Invoke(kvp.Value, null);
        }
    }
    #endregion

    #region 池管理
    private void EnsurePoolExists<T>(string poolName, T prefab) where T : Component
    {
        if (_pools.ContainsKey(poolName)) return;

        // 创建父物体用于组织对象
        Transform parent = CreateParentTransform(poolName);
        _parentTransforms.Add(poolName, parent);

        // 创建对象池
        ObjectPool<T> pool = new ObjectPool<T>
        (
            createFunc: () => CreateObject(prefab, parent),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject,
            collectionCheck: true,
            defaultCapacity: DEFAULT_CAPACITY,
            maxSize: DEFAULT_MAX_SIZE
        );

        _pools.Add(poolName, pool);
        // Debug.Log($"ObjectPool: 为预制体 '{poolName}' 创建新的对象池");
    }

    private string GetPoolKey<T>(T prefab) where T : Component
    {
        //return $"{typeof(T).Name}_{prefab.name}";
        return $"{prefab.name}";
    }
    private string GetPoolKeyForObjName(string objName)
    {
        // 通过对象名字找到对应的池（去掉"(Clone)"后缀）
        string objectName = objName.Replace("(Clone)", "");
        return $"{objectName}";
    }

    private Transform CreateParentTransform(string poolName)
    {
        GameObject parentObj = new GameObject($"Pool_{poolName}");
        parentObj.transform.SetParent(transform);
        return parentObj.transform;
    }

    private T CreateObject<T>(T prefab, Transform parent) where T : Component
    {
        T instance = Instantiate(prefab, parent);
        return instance;
    }
    #endregion

    #region 对象池回调
    protected virtual void OnGetObject<T>(T obj) where T : Component
    {
        obj.gameObject.SetActive(true);
    }

    protected virtual void OnReleaseObject<T>(T obj) where T : Component
    {
        obj.gameObject.SetActive(false);
    }

    protected virtual void OnDestroyObject<T>(T obj) where T : Component
    {
        if (obj != null)
        {
            Destroy(obj.gameObject);
        }
    }
    #endregion

    #region 私有方法
    private System.Collections.IEnumerator AutoReleaseCoroutine<T>(T obj, float releaseTime) where T : Component
    {
        yield return new WaitForSeconds(releaseTime);

        if (obj != null)
        {
            Release(obj);
        }
    }
    #endregion

    #region 信息查询
    public string GetPoolInfo<T>(T prefab) where T : Component
    {
        if (prefab == null) return "预制体为空";

        string poolName = GetPoolKey<T>(prefab);
        if (!_pools.ContainsKey(poolName))
        {
            return $"预制体 '{poolName}' 的对象池不存在";
        }

        return $"对象池 '{poolName}': 容量={DEFAULT_CAPACITY}, 最大={DEFAULT_MAX_SIZE}, 自动回收={DEFAULT_AUTO_RELEASE_TIME}秒";
    }

    public bool HasPool<T>(T prefab) where T : Component
    {
        if (prefab == null) return false;
        return _pools.ContainsKey(GetPoolKey<T>(prefab));
    }
    public bool HasPool(string objectName)
    {
        return _pools.ContainsKey(GetPoolKeyForObjName(objectName));
    }

    public List<string> GetAllPoolNames()
    {
        return new List<string>(_pools.Keys);
    }
    #endregion

    #region 编辑器功能

#if UNITY_EDITOR
    [Title("调试信息")]
    [Button("打印所有对象池信息")]
    private void DebugPrintAllPoolInfo()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("请在运行时查看对象池信息");
            return;
        }

        Debug.Log("=== ObjectPool 信息 ===");
        Debug.Log($"已创建的对象池数量: {_pools.Count}");

        foreach (var poolName in _pools.Keys)
        {
            Debug.Log($"对象池 '{poolName}': 容量={DEFAULT_CAPACITY}, 最大={DEFAULT_MAX_SIZE}");
        }
    }

    [Button("清空所有对象池")]
    private void DebugClearAllPools()
    {
        if (!Application.isPlaying) return;

        ClearAll();
        Debug.Log("已清空所有对象池");
    }
#endif
    #endregion
}
