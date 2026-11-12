# 白底酒红描边箭头

当前状态：自定义光标已停用，游戏使用系统默认指针；本目录仅保留源稿。

运行素材位于 `Assets/DraculasBox/Art/Resources/Cursor/`，本目录保留三态 SVG。

Windows 下 Unity 6 存在 32px 硬件光标模糊的已记录问题（UUM-120735 / #6613），仅改用 Point 或重画低分辨率像素不能解决最终显示链路。当前按该问题的 64px 方案，由连续矢量直接绘制高清三态，禁止再导入为 32px；无压缩、无 mipmap，保留 Point 过滤。

64×64 资源内可见轮廓为 24×36 资源像素，对应名义 32×32 画布内的 12×18 轮廓。该尺寸关系不等于现场硬件光标验收结果。资源内尖端特意对齐 `(3, 2)`，因此沿用现有代码热点；三态轮廓一致，仅改变白底和酒红边的明暗。

导入、轮廓与热点由原生 Unity 检查；未为检查而切换系统光标或接管鼠标。最终屏幕大小与清晰度仍需正常试玩核对。

依据：https://issuetracker-mig.prd.it.unity3d.com/issues/cursor-size-and-resolution-is-inaccurate-when-switching-the-default-cursor-field
