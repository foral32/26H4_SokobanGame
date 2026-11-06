# 后处理描边
Unity 6000.3.6f1 / URP 17.3。沿用现有 Renderer Feature，不覆盖 Renderer、Pipeline 或相机配置。
线宽独立于几何检测；minimumFeatureSize 为薄层和细槽的世界尺度过滤值，0 可关闭。
内部结构线支持距离衰减，AA 在轮廓与合成边缘局部解析，不提供时域历史补偿。
检查 RenderGraph 和兼容模式，覆盖薄贴层、曲面、门框、台阶、天空和远近运动。
Shader 使用 Hidden/PostProcessing/PostProcessOutline；Player 构建需检查 Shader 收录。
暗角脉冲使用 PostProcessManager.PlayVignettePulse，结束恢复原颜色、强度及 override 状态。
重复脉冲、常驻暗角与停用恢复需一起验证，不要求修改模板 Volume Profile。
