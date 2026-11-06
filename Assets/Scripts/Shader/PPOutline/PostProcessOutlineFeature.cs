using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace PostProcessing.Outline
{
    [System.Serializable]
    public class OutlineSettings
    {
        [Header("Outline Appearance")]
        [Tooltip("输出像素线宽（0~5）；只影响成线，不改变几何检测范围。推荐 1~2，0 为不绘制。")]
        public float outlineThickness = 1.0f;

        [Tooltip("描边颜色，通常用纯黑 (0,0,0,1)。")]
        public Color outlineColor = Color.black;

        [Header("Edge Detection Thresholds")]
        [Tooltip("遮挡轮廓阈值；综合表面平面残差、像素足迹和相对深度间隔，排除斜面坡度及细槽。推荐 0.08~0.15。")]
        public float depthThreshold = 0.1f;

        [Tooltip("稳定结构折角的法线阈值；比较两侧面及其内部一致性，不直接对单对像素的夹角取最大值。推荐 0.3~0.5。")]
        public float normalThreshold = 0.4f;

        [Tooltip("颜色差异边缘检测阈值。需同时开启 Use Color Difference。推荐 0.2~0.5。")]
        public float colorThreshold = 0.2f;

        [Header("Edge Detection Sensitivity")]
        [Tooltip("表面平面残差的放大系数；越高越容易检出深度边界。")]
        public float depthSensitivity = 1.0f;

        [Tooltip("法线夹角差放大系数。值越高，较小的折角也会产生内部描边。")]
        public float normalsSensitivity = 1.0f;

        [Tooltip("颜色 Sobel 梯度放大系数。需同时开启 Use Color Difference，推荐 0.05~0.2。")]
        public float colorSensitivity = 1.0f;

        [Header("Enhanced Detection")]
        [Tooltip("遮挡检测增加四个对角方向，改善斜向轮廓连续性；不改变法线结构筛选。")]
        public bool useEnhancedDetection = true;

        [Tooltip("对角深度响应的权重；保留旧字段兼容，2 为与水平/垂直方向等权。")]
        public float robertsWeight = 2.0f;

        [Tooltip("深度残差响应权重；不用于恢复或放大被筛除的高频法线噪声。")]
        public float maxDiffWeight = 1.5f;

        #region 内部边缘距离控制

        [Header("Internal Edge Control")]
        [Tooltip("启用后，内部深度、法线和颜色边缘会按距离逐渐淡出；关闭后，各距离使用一致的描边强度。外轮廓始终不受影响。")]
        public bool useInternalEdgeDistanceFade = true;

        [Tooltip("折角两侧稳定面的深度支持门限；交线由两面重建，不将曲面细槽单独升级为内部线。")]
        [Range(0.0001f, 0.1f)]
        public float internalDepthThreshold = 0.01f;

        [Tooltip("内部细节的世界尺度（米）。近乎共面的薄贴层/细槽低于此尺度时淡出，真实遮挡与较大结构保留；0 关闭此筛选。")]
        public float minimumFeatureSize = 0.06f;

        [Tooltip("内部深度、法线和颜色边缘从该眼空间距离开始衰减。外轮廓不受影响。")]
        [Min(0f)]
        public float internalEdgeFadeStart = 15f;

        [Tooltip("内部边缘到该眼空间距离时完全消失，用于抑制远处楼梯等高频细节。")]
        [Min(0.01f)]
        public float internalEdgeFadeEnd = 35f;

        [Tooltip("检测软阈值宽度；限制在阈值一半以内，保证无边缘处不改变场景。不是时域抗锯齿。")]
        [Range(0.0001f, 0.2f)]
        public float edgeSoftness = 0.02f;

        #endregion

        [Header("Detection Channels")]
        [Tooltip("启用深度 + 法线双通道边缘检测。这是描边的核心，建议保持开启。")]
        public bool useDepthNormals = true;

        [Tooltip("启用颜色差异边缘检测。可补充颜色突变处的描边，但可能误判阴影边缘，按需开启。")]
        public bool useColorDifference = true;

        [Header("Quality")]
        [Tooltip("结构线使用亚像素覆盖率；轮廓及合成后的边缘局部使用 FXAA。非边缘原色不变，不改变相机 AA，无历史帧处理。")]
        public bool enableAntiAliasing = true;
    }

    public class PostProcessOutlineFeature : ScriptableRendererFeature
    {
        public OutlineSettings settings = new OutlineSettings();
        private PostProcessOutlinePass m_OutlinePass;
        private Material m_OutlineMaterial;

        public override void Create()
        {
            m_OutlinePass?.Dispose();
            CoreUtils.Destroy(m_OutlineMaterial);
            m_OutlineMaterial = CoreUtils.CreateEngineMaterial("Hidden/PostProcessing/PostProcessOutline");
            if (m_OutlineMaterial == null)
            {
                Debug.LogError("[Outline] FAILED to create material! Shader 'Hidden/PostProcessing/PostProcessOutline' not found. " +
                               "Register it in Edit → Project Settings → Graphics → Always Included Shaders.");
            }

            m_OutlinePass = new PostProcessOutlinePass(m_OutlineMaterial, settings)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_OutlineMaterial == null || m_OutlinePass == null) return;

            m_OutlinePass.ConfigureInput(ScriptableRenderPassInput.Color
                                       | ScriptableRenderPassInput.Depth
                                       | ScriptableRenderPassInput.Normal);
            renderer.EnqueuePass(m_OutlinePass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // 兼容模式的相机颜色目标只能在分配完成后获取。
            m_OutlinePass?.Setup(renderer.cameraColorTargetHandle);
        }

        protected override void Dispose(bool disposing)
        {
            m_OutlinePass?.Dispose();
            m_OutlinePass = null;
            CoreUtils.Destroy(m_OutlineMaterial);
            m_OutlineMaterial = null;
        }
    }

    public class PostProcessOutlinePass : ScriptableRenderPass
    {
        private readonly Material m_Material;
        private readonly OutlineSettings m_Settings;
        private RTHandle m_TempRT;
        private RTHandle m_EdgeMaskRT;
        private RTHandle m_FilteredMaskRT;

        static readonly int s_OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        static readonly int s_DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        static readonly int s_NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        static readonly int s_ColorThreshold = Shader.PropertyToID("_ColorThreshold");
        static readonly int s_OutlineColor = Shader.PropertyToID("_OutlineColor");
        static readonly int s_DepthSensitivity = Shader.PropertyToID("_DepthSensitivity");
        static readonly int s_NormalsSensitivity = Shader.PropertyToID("_NormalsSensitivity");
        static readonly int s_ColorSensitivity = Shader.PropertyToID("_ColorSensitivity");
        static readonly int s_RobertsWeight = Shader.PropertyToID("_RobertsWeight");
        static readonly int s_MaxDiffWeight = Shader.PropertyToID("_MaxDiffWeight");
        static readonly int s_InternalDepthThreshold = Shader.PropertyToID("_InternalDepthThreshold");
        static readonly int s_MinimumFeatureSize = Shader.PropertyToID("_MinimumFeatureSize");
        static readonly int s_InternalEdgeFadeStart = Shader.PropertyToID("_InternalEdgeFadeStart");
        static readonly int s_InternalEdgeFadeEnd = Shader.PropertyToID("_InternalEdgeFadeEnd");
        static readonly int s_EdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
        static readonly int s_OutlineSourceTexture = Shader.PropertyToID("_OutlineSourceTexture");
        static readonly int s_OutlineHasNormals = Shader.PropertyToID("_OutlineHasNormals");
        static readonly int s_OutlineCoverageTexture = Shader.PropertyToID("_OutlineCoverageTexture");

        public PostProcessOutlinePass(Material material, OutlineSettings settings)
        {
            m_Material = material;
            m_Settings = settings;
            profilingSampler = new ProfilingSampler("Post Process Outline");
        }

        public void Dispose()
        {
            m_TempRT?.Release();
            m_TempRT = null;
            m_EdgeMaskRT?.Release();
            m_EdgeMaskRT = null;
            m_FilteredMaskRT?.Release();
            m_FilteredMaskRT = null;
        }

        #region RenderGraph 数据

        private class PassData
        {
            public TextureHandle cameraColor;
            public TextureHandle cameraDepth;
            public TextureHandle cameraNormals;
            public TextureHandle tempColor;
            public TextureHandle edgeMask;
            public TextureHandle filteredMask;
            public bool hasNormals;
            public Material material;
            public bool resolveEdgeColor;
        }

        #endregion

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null) return;

            UpdateMaterialProperties();

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle cameraColor = resourceData.activeColorTexture;
            if (!cameraColor.IsValid()) return;

            TextureHandle cameraDepth = resourceData.cameraDepthTexture;
            if (!cameraDepth.IsValid()) return;

            TextureHandle cameraNormals = resourceData.cameraNormalsTexture;
            bool hasNormals = cameraNormals.IsValid();

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempRT, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_OutlineTempRT");
            // 遮罩是覆盖率，不使用相机 HDR 格式或 sRGB 转换。
            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_EdgeMaskRT, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_OutlineEdgeMaskRT");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_FilteredMaskRT, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_OutlineFilteredMaskRT");

            TextureHandle tempHandle = renderGraph.ImportTexture(m_TempRT);
            TextureHandle edgeMaskHandle = renderGraph.ImportTexture(m_EdgeMaskRT);
            TextureHandle filteredMaskHandle = renderGraph.ImportTexture(m_FilteredMaskRT);

            using (var builder = renderGraph.AddUnsafePass<PassData>("Post Process Outline", out var passData, profilingSampler))
            {
                passData.cameraColor = cameraColor;
                passData.cameraDepth = cameraDepth;
                passData.cameraNormals = cameraNormals;
                passData.tempColor = tempHandle;
                passData.edgeMask = edgeMaskHandle;
                passData.filteredMask = filteredMaskHandle;
                passData.hasNormals = hasNormals;
                passData.material = m_Material;
                passData.resolveEdgeColor = m_Settings.enableAntiAliasing;

                builder.UseTexture(cameraColor, AccessFlags.ReadWrite);
                builder.UseTexture(cameraDepth, AccessFlags.Read);
                builder.UseTexture(tempHandle, AccessFlags.ReadWrite);
                builder.UseTexture(edgeMaskHandle, AccessFlags.ReadWrite);
                builder.UseTexture(filteredMaskHandle, AccessFlags.ReadWrite);
                if (hasNormals)
                    builder.UseTexture(cameraNormals, AccessFlags.Read);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    cmd.SetGlobalTexture("_CameraDepthTexture", data.cameraDepth);
                    if (data.hasNormals)
                        cmd.SetGlobalTexture("_CameraNormalsTexture", data.cameraNormals);
                    cmd.SetGlobalFloat(s_OutlineHasNormals, data.hasNormals ? 1f : 0f);

                    Blitter.BlitCameraTexture(cmd, data.cameraColor, m_TempRT);
                    Blitter.BlitCameraTexture(cmd, m_TempRT, m_EdgeMaskRT, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, m_EdgeMaskRT, m_FilteredMaskRT, data.material, 1);
                    cmd.SetGlobalTexture(s_OutlineSourceTexture, data.tempColor);
                    Blitter.BlitCameraTexture(cmd, m_FilteredMaskRT, data.cameraColor, data.material, 2);
                    if (data.resolveEdgeColor)
                    {
                        // 遮罩平滑不能消除原色在同一几何边上的亮色锯齿；只解析边缘局部。
                        Blitter.BlitCameraTexture(cmd, data.cameraColor, m_TempRT);
                        cmd.SetGlobalTexture(s_OutlineCoverageTexture, data.filteredMask);
                        Blitter.BlitCameraTexture(cmd, m_TempRT, data.cameraColor, data.material, 3);
                    }
                });
            }
        }

        private RTHandle m_Source;

        public void Setup(RTHandle source) => m_Source = source;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_OutlineTempRT");
            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            RenderingUtils.ReAllocateIfNeeded(ref m_EdgeMaskRT, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_OutlineEdgeMaskRT");
            RenderingUtils.ReAllocateIfNeeded(ref m_FilteredMaskRT, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_OutlineFilteredMaskRT");
            ConfigureTarget(m_Source);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null || m_Source == null || m_TempRT == null || m_EdgeMaskRT == null || m_FilteredMaskRT == null) return;
            UpdateMaterialProperties();

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.SetGlobalFloat(s_OutlineHasNormals, 1f);
                Blitter.BlitCameraTexture(cmd, m_Source, m_TempRT);
                Blitter.BlitCameraTexture(cmd, m_TempRT, m_EdgeMaskRT, m_Material, 0);
                Blitter.BlitCameraTexture(cmd, m_EdgeMaskRT, m_FilteredMaskRT, m_Material, 1);
                cmd.SetGlobalTexture(s_OutlineSourceTexture, m_TempRT);
                Blitter.BlitCameraTexture(cmd, m_FilteredMaskRT, m_Source, m_Material, 2);
                if (m_Settings.enableAntiAliasing)
                {
                    Blitter.BlitCameraTexture(cmd, m_Source, m_TempRT);
                    cmd.SetGlobalTexture(s_OutlineCoverageTexture, m_FilteredMaskRT);
                    Blitter.BlitCameraTexture(cmd, m_TempRT, m_Source, m_Material, 3);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) => m_Source = null;

        private void UpdateMaterialProperties()
        {
            if (m_Material == null) return;
            m_Material.SetFloat(s_OutlineThickness, Mathf.Clamp(m_Settings.outlineThickness, 0f, 5f));
            m_Material.SetFloat(s_DepthThreshold, m_Settings.depthThreshold);
            m_Material.SetFloat(s_NormalThreshold, m_Settings.normalThreshold);
            m_Material.SetFloat(s_ColorThreshold, m_Settings.colorThreshold);
            m_Material.SetColor(s_OutlineColor, m_Settings.outlineColor);
            m_Material.SetFloat(s_DepthSensitivity, m_Settings.depthSensitivity);
            m_Material.SetFloat(s_NormalsSensitivity, m_Settings.normalsSensitivity);
            m_Material.SetFloat(s_ColorSensitivity, m_Settings.colorSensitivity);
            m_Material.SetFloat(s_RobertsWeight, m_Settings.robertsWeight);
            m_Material.SetFloat(s_MaxDiffWeight, m_Settings.maxDiffWeight);
            m_Material.SetFloat(s_InternalDepthThreshold, m_Settings.internalDepthThreshold);
            m_Material.SetFloat(s_MinimumFeatureSize, Mathf.Max(0f, m_Settings.minimumFeatureSize));
            m_Material.SetFloat(s_InternalEdgeFadeStart, m_Settings.internalEdgeFadeStart);
            m_Material.SetFloat(s_InternalEdgeFadeEnd,
                Mathf.Max(m_Settings.internalEdgeFadeEnd, m_Settings.internalEdgeFadeStart + 0.01f));
            m_Material.SetFloat(s_EdgeSoftness, m_Settings.edgeSoftness);

            SetKeyword("_USE_DEPTH_NORMALS", m_Settings.useDepthNormals);
            SetKeyword("_USE_COLOR_DIFFERENCE", m_Settings.useColorDifference);
            SetKeyword("_ENABLE_ANTI_ALIASING", m_Settings.enableAntiAliasing);
            SetKeyword("_USE_ENHANCED_DETECTION", m_Settings.useEnhancedDetection);
            SetKeyword("_USE_INTERNAL_EDGE_DISTANCE_FADE", m_Settings.useInternalEdgeDistanceFade);
        }

        private void SetKeyword(string keyword, bool enable)
        {
            if (enable) m_Material.EnableKeyword(keyword);
            else m_Material.DisableKeyword(keyword);
        }
    }
}
