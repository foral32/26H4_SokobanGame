# 作品演示交互原型

当前为第二稿，改为 Unity 原生预览。在 Edit Mode 通过菜单 `Tools → Dracula's Box → Prototypes → 作品演示` 打开，不需要启动 Play，不读写游戏存档。

点击屏幕右边缘的手账索引签进入；TD／LD 切换分组，箭头和左右键翻页。空格切换静帧轮播，悬停画面显示放大入口，Esc 返回。只有索引签和引导弹窗可操作，原主菜单用截图示意。

完整设计见 [作品演示交互原型](../../../Document/Design/作品演示交互原型.md)。显示内容与后续录制提纲见 `content.json`。现有图片均为静帧，尚未录制视频。

新评审截图和动效在 `DraculasBox/Reviews/Showcase-20261111-r2/`。原生预览代码为 `Assets/DraculasBox/Editor/Prototypes/ShowcaseReviewWindow.cs`，独立临时场景不进入正式场景或 Player。第一稿 Windows 宿主归档于 `DraculasBox/Reviews/Showcase-20261111/source-v1/`，不再使用。
