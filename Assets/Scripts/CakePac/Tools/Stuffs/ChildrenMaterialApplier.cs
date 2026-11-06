using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Flags]
public enum RendererType
{
    SpriteRenderer = 1 << 0,
    MeshRenderer = 1 << 1,
    SkinnedMeshRenderer = 1 << 2,
    LineRenderer = 1 << 3,
    TrailRenderer = 1 << 4,
    ParticleSystemRenderer = 1 << 5
}

public class ChildrenMaterialApplier : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField, OnValueChanged("OnMaterialChanged")]
    private Material targetMaterial;

    [Header("Renderer Settings")]
    [SerializeField]
    private RendererType rendererTypes = RendererType.SpriteRenderer | RendererType.MeshRenderer | RendererType.SkinnedMeshRenderer;

    [SerializeField]
    private bool includeInactive = true;

    [SerializeField]
    private bool applyToSelf = true;

    [Header("Material Index Settings")]
    [SerializeField, Tooltip("应用到材质数组的哪个索引，-1表示应用到所有材质")]
    private int materialIndex = -1;

    [Header("Debug")]
    [SerializeField, ReadOnly]
    private int affectedRenderersCount;

    // 缓存的渲染器组件
    [ShowInInspector] private List<Renderer> cachedRenderers = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    #region Unity Lifecycle

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyMaterial();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 设置目标材质
    /// </summary>
    /// <param name="material">要应用的材质</param>
    public void SetMaterial(Material material)
    {
        targetMaterial = material;
        ApplyMaterial();
    }

    /// <summary>
    /// 刷新渲染器缓存
    /// </summary>
    public void RefreshRenderers()
    {
        cachedRenderers.Clear();
        originalMaterials.Clear();

        // 获取所有指定类型的渲染器
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(includeInactive);

        foreach (Renderer renderer in allRenderers)
        {
            // 检查是否包含自身
            if (!applyToSelf && renderer.gameObject == gameObject)
                continue;

            // 检查渲染器类型
            if (IsRendererTypeIncluded(renderer))
            {
                cachedRenderers.Add(renderer);
                // 保存原始材质
                originalMaterials[renderer] = renderer.sharedMaterials;
            }
        }

        affectedRenderersCount = cachedRenderers.Count;
    }

    /// <summary>
    /// 应用材质到所有缓存的渲染器
    /// </summary>
    [Button("Apply Material")]
    public void ApplyMaterial()
    {
        if (targetMaterial == null)
        {
            return;
        }
        RefreshRenderers();
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null) continue;

            ApplyMaterialToRenderer(renderer);
        }
    }

    /// <summary>
    /// 恢复所有渲染器的原始材质
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 当材质改变时调用
    /// </summary>
    private void OnMaterialChanged()
    {
        if (Application.isPlaying)
        {
            ApplyMaterial();
        }
    }

    /// <summary>
    /// 检查渲染器类型是否被包含
    /// </summary>
    /// <param name="renderer">要检查的渲染器</param>
    /// <returns>是否包含该类型</returns>
    private bool IsRendererTypeIncluded(Renderer renderer)
    {
        if (renderer is SpriteRenderer && (rendererTypes & RendererType.SpriteRenderer) != 0)
            return true;
        if (renderer is MeshRenderer && (rendererTypes & RendererType.MeshRenderer) != 0)
            return true;
        if (renderer is SkinnedMeshRenderer && (rendererTypes & RendererType.SkinnedMeshRenderer) != 0)
            return true;
        if (renderer is LineRenderer && (rendererTypes & RendererType.LineRenderer) != 0)
            return true;
        if (renderer is TrailRenderer && (rendererTypes & RendererType.TrailRenderer) != 0)
            return true;
        if (renderer is ParticleSystemRenderer && (rendererTypes & RendererType.ParticleSystemRenderer) != 0)
            return true;

        return false;
    }

    /// <summary>
    /// 应用材质到指定渲染器
    /// </summary>
    /// <param name="renderer">目标渲染器</param>
    private void ApplyMaterialToRenderer(Renderer renderer)
    {
        //        Debug.Log($"Applying material to renderer: {renderer.name}");
        Material[] materials = renderer.sharedMaterials;

        if (materialIndex == -1)
        {
            // 应用到所有材质
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = targetMaterial;
            }
        }
        else
        {
            // 应用到指定索引的材质
            if (materialIndex >= 0 && materialIndex < materials.Length)
            {
                materials[materialIndex] = targetMaterial;
            }
            else
            {
                Debug.LogWarning($"Material index {materialIndex} is out of range for renderer {renderer.name}");
                return;
            }
        }

        renderer.materials = materials;
    }

    #endregion

    #region Editor Helpers


    #endregion
}