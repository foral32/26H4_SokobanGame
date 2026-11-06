using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 粒子管理器
/// </summary>
public class ParticleManager : Singleton<ParticleManager>
{
    [System.Serializable]
    private class ParticleInfo
    {
        [HorizontalGroup("Row1", 0.6f), LabelWidth(60), LabelText("特效名称"), Tooltip("用于在代码中访问")]
        public string vfx_name;
        [HorizontalGroup("Row1", 0.4f), LabelWidth(60), LabelText("释放等待"), Tooltip("特效释放前等待时间(秒)"), SuffixLabel("秒")]
        public float waitTimeForRelease = 5f;

        [LabelText("特效预制体"), LabelWidth(70), OnValueChanged("UpdateNameFromPrefab")]
        public ParticleSystem vfx_prefab;

        // 当预制体被分配时自动更新名称
        private void UpdateNameFromPrefab()
        {
            if (vfx_prefab != null) vfx_name = vfx_prefab.name;
        }
    }

    [Header("特效配置")]
    [SerializeField] private ParticleInfo[] particleInfos;

    [Header("对象池设置")]
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 50;

    private List<ObjectPool<ParticleSystem>> _poolList = new List<ObjectPool<ParticleSystem>>();
    private Dictionary<string, int> _particleDic = new Dictionary<string, int>();

    protected override void ManagedInitialize()
    {
        base.ManagedInitialize();
        InitializeParticles();
    }

    private void InitializeParticles()
    {
        if (particleInfos == null || particleInfos.Length == 0)
        {
            return;
        }

        // 初始化对象池和字典
        for (int i = 0; i < particleInfos.Length; i++)
        {
            var info = particleInfos[i];
            if (info.vfx_prefab == null)
            {
                Debug.LogWarning($"ParticleManager: 特效 {info.vfx_name} 预制体为空!");
                continue;
            }

            // 创建父物体
            var parent = new GameObject(info.vfx_name).transform;
            parent.SetParent(transform);

            // 创建对象池
            ObjectPool<ParticleSystem> pool = new ObjectPool<ParticleSystem>
            (
                () => Instantiate(info.vfx_prefab, parent),
                v => v.gameObject.SetActive(true),
                v => v.gameObject.SetActive(false),
                v => Destroy(v.gameObject),
                true,
                _defaultCapacity,
                _maxSize
            );

            _poolList.Add(pool);
            _particleDic.Add(info.vfx_name, i);
        }
    }

    #region 播放粒子 API
    public ParticleSystem PlayOneShot(string Name, Vector3 pos)
    {
        return PlayOneShot(Name, pos, Vector2.zero);
    }

    public ParticleSystem PlayOneShot(string Name, Vector3 pos, Vector2 normal)
    {
        if (string.IsNullOrEmpty(Name)) return null;
        var index = GetParticleIndex(Name);
        if (index < 0)
        {
            Debug.LogWarning($"ParticleManager: 无法找到特效 {Name} 的索引!");
            return null;
        }

        var particle = _poolList[index].Get();
        SetParticleTransform(particle.transform, pos, normal);
        particle.Play();
        StartCoroutine(AfterPlayCor(index, particle));
        return particle;
    }

    public ParticleSystem PlayOneShot(string Name, Vector3 pos, Vector2 normal, Color color)
    {
        var index = GetParticleIndex(Name);
        if (index < 0) return null;

        var particle = _poolList[index].Get();
        SetParticleTransform(particle.transform, pos, normal);
        SetParticleColor(particle, color);
        particle.Play();

        StartCoroutine(AfterPlayCor(index, particle));
        return particle;
    }
    #endregion
    #region 私有方法
    /// <summary>
    /// 获取粒子编号
    /// </summary>
    private int GetParticleIndex(string Name)
    {
        if (!_particleDic.ContainsKey(Name))
        {
            print("字典中无此ID: Name = " + Name);
            return -1;
        }

        int index = _particleDic[Name];
        return index;
    }
    /// <summary>
    /// 设置变换
    /// </summary>
    private void SetParticleTransform(Transform transform, Vector3 pos, Vector2 normal)
    {
        transform.position = pos;
        // ​将物体的旋转调整为使其局部坐标系的“上方向”（Y轴）对齐到指定的法线方向
        if (normal != Vector2.zero) // 检查法线是否为零向量,若是则忽略旋转设置
            transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    private void SetParticleColor(ParticleSystem particle, Color color)
    {
        var main = particle.main;
        main.startColor = color;
    }

    private IEnumerator AfterPlayCor(int index, ParticleSystem particle)
    {
        float wait = particleInfos[index].waitTimeForRelease;
        yield return new WaitForSeconds(wait);

        _poolList[index].Release(particle);
    }
    #endregion

    #region 编辑器功能
    private void OnValidate()
    {
        // 确保每个特效的名称与其预制体保持同步
        if (particleInfos != null)
        {
            for (int i = 0; i < particleInfos.Length; i++)
            {
                var info = particleInfos[i];
                // 检查预制体是否存在且名称为空
                if (info.vfx_prefab != null && string.IsNullOrEmpty(info.vfx_name))
                {
                    info.vfx_name = info.vfx_prefab.name;
                }
            }
        }
    }
    #endregion
}
