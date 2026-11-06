using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Outline
{
    [System.Serializable]
    public class OutlineController : MonoBehaviour
    {
        [Header("Feature Reference")]
        [Tooltip("方式1: 手动分配 - 从Renderer Asset中找到对应的Feature并拖拽到这里")]
        public PostProcessOutlineFeature outlineFeature;

        [Header("Auto Detection")]
        [Tooltip("方式2: 自动检测 - 启用后会自动在渲染器中查找PostProcessOutlineFeature")]
        public bool autoDetectFeature = true;

        [Tooltip("指定要搜索的Renderer Asset，留空则使用当前相机的渲染器")]
        public UniversalRendererData targetRenderer;

        [Header("Outline Control")]
        public bool enableOutline = true;

        [Header("Basic Settings")]
        [Range(0.1f, 5.0f)]
        [Tooltip("输出像素线宽，独立于检测核；改变粗细不改变哪些几何被检出。")]
        public float outlineThickness = 1.0f;

        [ColorUsage(false)]
        public Color outlineColor = Color.black;

        [Header("Detection Thresholds")]
        [Range(0.0f, 1.0f)]
        public float depthThreshold = 0.1f;

        [Range(0.0f, 1.0f)]
        public float normalThreshold = 0.4f;

        [Range(0.0f, 1.0f)]
        public float colorThreshold = 0.2f;

        [Header("Sensitivity")]
        [Range(0.1f, 10.0f)]
        public float depthSensitivity = 1.0f;

        [Range(0.1f, 10.0f)]
        public float normalsSensitivity = 1.0f;

        [Range(0.1f, 10.0f)]
        public float colorSensitivity = 1.0f;

        [Header("Feature Toggles")]
        public bool useDepthNormals = true;
        public bool useColorDifference = true;
        [Tooltip("几何折线亚像素覆盖率 + 轮廓/合成边缘局部 FXAA；非边缘原色不变，不改变相机 AA，无历史帧处理")]
        public bool enableAntiAliasing = true;

        [Header("Enhanced Detection")]
        [Tooltip("遮挡检测增加四个对角方向，改善斜向轮廓连续性")]
        public bool useEnhancedDetection = true;

        [Range(0.1f, 5.0f)]
        [Tooltip("对角深度响应权重，2 与水平/垂直方向等权；旧字段名保留兼容")]
        public float robertsWeight = 2.0f;

        [Range(0.1f, 5.0f)]
        [Tooltip("表面平面残差的深度响应权重")]
        public float maxDiffWeight = 1.5f;

        #region 内部边缘距离控制

        [Header("内部边缘距离控制")]
        [Tooltip("启用后内部描边按距离衰减；关闭后远近描边保持一致强度")]
        public bool useInternalEdgeDistanceFade = true;

        [Range(0.0001f, 0.1f)]
        [Tooltip("内部小深度折角阈值，需要同时有稳定法线结构支持")]
        public float internalDepthThreshold = 0.01f;

        [Tooltip("内部薄层/细槽的世界尺度（米）；0 关闭细节筛选，不影响天空轮廓。")]
        public float minimumFeatureSize = 0.06f;

        [Min(0f)]
        [Tooltip("内部描边开始随距离衰减的位置")]
        public float internalEdgeFadeStart = 15f;

        [Min(0.01f)]
        [Tooltip("内部描边完全消失的位置，外轮廓不受影响")]
        public float internalEdgeFadeEnd = 35f;

        [Range(0.0001f, 0.2f)]
        [Tooltip("检测软阈值宽度；不替代时域抗锯齿")]
        public float edgeSoftness = 0.02f;

        #endregion

        private void Start()
        {
            if (autoDetectFeature && outlineFeature == null)
            {
                FindOutlineFeature();
            }
        }

        private void Update()
        {
            if (outlineFeature != null)
            {
                UpdateOutlineSettings();
            }
            else if (autoDetectFeature)
            {
                FindOutlineFeature();
            }
        }

        void OnValidate()
        {
            if (outlineFeature != null)
            {
                UpdateOutlineSettings();
            }
        }

        /// <summary>
        /// 自动查找PostProcessOutlineFeature
        /// </summary>
        private void FindOutlineFeature()
        {
            // 方法1: 从指定的Renderer Asset中查找
            if (targetRenderer != null)
            {
                outlineFeature = FindFeatureInRenderer(targetRenderer);
                if (outlineFeature != null)
                {
                    Debug.Log($"Found PostProcessOutlineFeature in specified renderer: {targetRenderer.name}");
                    return;
                }
            }

            // 方法2: 从当前相机的渲染器中查找
            var camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                var cameraData = camera.GetUniversalAdditionalCameraData();
                if (cameraData != null)
                {
                    var renderer = cameraData.scriptableRenderer;
                    if (renderer is UniversalRenderer universalRenderer)
                    {
                        // 通过反射获取rendererFeatures
                        var rendererDataField = typeof(UniversalRenderer).GetField("m_RendererData",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (rendererDataField != null)
                        {
                            var rendererData = rendererDataField.GetValue(universalRenderer) as UniversalRendererData;
                            if (rendererData != null)
                            {
                                outlineFeature = FindFeatureInRenderer(rendererData);
                                if (outlineFeature != null)
                                {
                                    Debug.Log($"Found PostProcessOutlineFeature in camera renderer: {rendererData.name}");
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // 方法3: 从URP Pipeline Asset中的所有渲染器查找
            var pipeline = UniversalRenderPipeline.asset;
            if (pipeline != null)
            {
                // 使用反射获取渲染器列表
                var rendererDataListProperty = typeof(UniversalRenderPipelineAsset).GetProperty("rendererDataList",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (rendererDataListProperty != null)
                {
                    var rendererDataList = rendererDataListProperty.GetValue(pipeline) as ScriptableRendererData[];
                    if (rendererDataList != null)
                    {
                        foreach (var rendererData in rendererDataList)
                        {
                            if (rendererData is UniversalRendererData universalRendererData)
                            {
                                outlineFeature = FindFeatureInRenderer(universalRendererData);
                                if (outlineFeature != null)
                                {
                                    Debug.Log($"Found PostProcessOutlineFeature in pipeline renderer: {universalRendererData.name}");
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (outlineFeature == null)
            {
                Debug.LogWarning("PostProcessOutlineFeature not found in any renderer. Please add it to your URP Renderer Asset or assign it manually.");
            }
        }

        /// <summary>
        /// 在指定的渲染器中查找PostProcessOutlineFeature
        /// </summary>
        private PostProcessOutlineFeature FindFeatureInRenderer(UniversalRendererData rendererData)
        {
            if (rendererData != null && rendererData.rendererFeatures != null)
            {
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is PostProcessOutlineFeature outlineFeature)
                    {
                        return outlineFeature;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 手动查找Feature的按钮方法
        /// </summary>
        [ContextMenu("Find Outline Feature")]
        public void ManualFindFeature()
        {
            FindOutlineFeature();
            if (outlineFeature != null)
            {
                Debug.Log("PostProcessOutlineFeature found and assigned!");
            }
            else
            {
                Debug.LogError("PostProcessOutlineFeature not found! Make sure it's added to a URP Renderer Asset.");
            }
        }

        private void UpdateOutlineSettings()
        {
            var settings = outlineFeature.settings;

            // 更新基本设置
            settings.outlineThickness = outlineThickness;
            settings.outlineColor = outlineColor;

            // 更新阈值
            settings.depthThreshold = depthThreshold;
            settings.normalThreshold = normalThreshold;
            settings.colorThreshold = colorThreshold;

            // 更新敏感度
            settings.depthSensitivity = depthSensitivity;
            settings.normalsSensitivity = normalsSensitivity;
            settings.colorSensitivity = colorSensitivity;

            // 更新功能开关
            settings.useDepthNormals = useDepthNormals;
            settings.useColorDifference = useColorDifference;
            settings.enableAntiAliasing = enableAntiAliasing;

            // 更新增强检测设置
            settings.useEnhancedDetection = useEnhancedDetection;
            settings.robertsWeight = robertsWeight;
            settings.maxDiffWeight = maxDiffWeight;
            settings.useInternalEdgeDistanceFade = useInternalEdgeDistanceFade;
            settings.internalDepthThreshold = internalDepthThreshold;
            settings.minimumFeatureSize = minimumFeatureSize;
            settings.internalEdgeFadeStart = internalEdgeFadeStart;
            settings.internalEdgeFadeEnd = Mathf.Max(internalEdgeFadeEnd, internalEdgeFadeStart + 0.01f);
            settings.edgeSoftness = edgeSoftness;

            // 启用/禁用特性
            outlineFeature.SetActive(enableOutline);
        }
        // 预设配置方法
        [ContextMenu("Apply My SimpleOntline Style")]
        public void ApplySimpleOntlineStyle()
        {
            outlineThickness = 1.0f;
            outlineColor = Color.black;
            depthThreshold = 0.1f;
            normalThreshold = 1f;
            colorThreshold = 1f;
            depthSensitivity = 1f;
            normalsSensitivity = 1f;
            colorSensitivity = 1f;
            useDepthNormals = true;
            useColorDifference = true;
            enableAntiAliasing = true;

            useEnhancedDetection = false;
            robertsWeight = 2.5f;
            maxDiffWeight = 2.0f;
        }

        // 预设配置方法
        [ContextMenu("Apply Cartoon Style")]
        public void ApplyCartoonStyle()
        {
            outlineThickness = 2.0f;
            outlineColor = Color.black;
            depthThreshold = 0.2f;
            normalThreshold = 0.5f;
            colorThreshold = 0.3f;
            depthSensitivity = 3.0f;
            normalsSensitivity = 3.0f;
            colorSensitivity = 2.0f;
            useDepthNormals = true;
            useColorDifference = true;
            enableAntiAliasing = true;

            // 卡通风格适合用增强检测
            useEnhancedDetection = true;
            robertsWeight = 2.5f;
            maxDiffWeight = 2.0f;
        }

        [ContextMenu("Apply Subtle Style")]
        public void ApplySubtleStyle()
        {
            outlineThickness = 0.8f;
            outlineColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            depthThreshold = 0.05f;
            normalThreshold = 0.2f;
            colorThreshold = 0.1f;
            depthSensitivity = 1.5f;
            normalsSensitivity = 1.5f;
            colorSensitivity = 1.0f;
            useDepthNormals = true;
            useColorDifference = false;
            enableAntiAliasing = true;

            // 细腻风格使用标准检测
            useEnhancedDetection = false;
            robertsWeight = 1.5f;
            maxDiffWeight = 1.0f;
        }

        [ContextMenu("Apply Technical Style")]
        public void ApplyTechnicalStyle()
        {
            outlineThickness = 1.5f;
            outlineColor = new Color(0.0f, 0.5f, 1.0f, 1.0f);
            depthThreshold = 0.15f;
            normalThreshold = 0.6f;
            colorThreshold = 0.4f;
            depthSensitivity = 2.5f;
            normalsSensitivity = 4.0f;
            colorSensitivity = 1.5f;
            useDepthNormals = true;
            useColorDifference = true;
            enableAntiAliasing = true;

            // 技术风格需要精确的边缘检测
            useEnhancedDetection = true;
            robertsWeight = 3.0f;
            maxDiffWeight = 2.5f;
        }

        [ContextMenu("Apply Distance Style")]
        public void ApplyDistanceStyle()
        {
            outlineThickness = 2.0f;
            outlineColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            depthThreshold = 0.1f;
            normalThreshold = 0.5f;
            colorThreshold = 1.0f;  // 避免阴影描边
            depthSensitivity = 3.0f;
            normalsSensitivity = 5.0f;
            colorSensitivity = 0.5f;
            useDepthNormals = true;
            useColorDifference = false;  // 关闭颜色检测
            enableAntiAliasing = true;

            // 距离检测优化设置
            useEnhancedDetection = true;
            robertsWeight = 5.0f;    // 增强Roberts算子权重
            maxDiffWeight = 4.0f;    // 增强最大差值检测
        }
    }
}
