# 作品演示素材

`Pages.json` 用 `sections` 配置 README、关卡设计、关卡编辑器、产品打磨四组页面。每页有 `id`、`title`、`guide`；可选 `clip` 指定录像，未填时沿用 `id`。README 为纯图文，不加载录像。其他页使用 `Clips/<clip或id>.webm` 和 JPG 封面。视频为原生 Unity 画面、无音轨、循环播放，观看不会执行录像里的操作。

重新录制：保存并退出普通 Play，在隔离的 `BackgroundPlaytest` 中等待主菜单，调用 `CampaignPlayChecks.RunShowcaseCapture()`。它使用临时玩家数据及正式目录副本，原始帧输出至 `Library/DraculasBoxShowcase/Frames/`。等待 `CampaignPlayChecks.Status` 为 `passed`，再运行 `python DraculasBox/Tools/Showcase/encode.py --ffmpeg <ffmpeg路径>`。编码为 WebM VP8／720p／20 fps，`CaptureManifest.json` 记录各片段长度。

本轮同时重录 `td04`，演示勾选机制与难度的严格匹配、采用草稿与撤销。录制等待的静止片段经过剪除，`CaptureManifest.json` 记录成片帧数及原始帧数。

`CampaignPlayChecks.RunShowcaseMediaRefreshCapture()` 仅重录主菜单（`pr01`）与编辑操作（`td01`）：左右键绘制／擦除、选择移动、越界扩边及行列增删。编码时使用 `--clips pr01 td01`，其他重点未变的视频继续沿用。`RunShowcaseReadmeRefresh()` 更新 README 中的主菜单与作品演示窗口截图；编辑器截图使用本轮 `td01.jpg` 封面。

`CampaignPlayChecks.RunShowcaseMenuCapture()` 单独录制主菜单悬停、章节往返和设置中的配色主题切换（`pr01`），并更新仓库 README 的主菜单实机图。编码时使用 `--clips pr01`。

`CampaignPlayChecks.RunShowcasePolishCapture()` 依次录制上述主菜单交互与章节完成片段（`pr01`、`pr02`），使用临时玩家进度，结束后恢复。编码时加 `--clips pr01 pr02` 只更新这两段，保留其他片段。动作反馈页复用 `ld01`。

`CampaignPlayChecks.RunShowcaseChecks()` 在隔离 Play 主菜单验证四组导航、正文布局、视频播放、暂停、输入拦截和关闭回收。普通 Play 中打开右侧「作品演示！」即可体验。

`RunShowcaseNavigationChecks()` 验证左右键连续遍历全部 15 页、双向跨页签、首尾停留、鼠标翻页与键盘一致，以及右下角正式编辑入口的定位、悬停和数据保护。

`RunShowcaseLayoutChecks()` 单独检查导航、文字布局、视频准备与资源回收，报告为 `showcase-layout.json`；它不检查视频时间推进，不能替代完整播放检查。

主菜单的两个工具入口以 `EditorOnly` 节点保存在场景中，未 Play 时也能看到外观；功能在 Play 中使用。若重建主菜单，可调用 `GameBuilder.RefreshEditorEntries()` 更新入口预览。整个素材目录只用于 Editor，功能代码通过 `UNITY_EDITOR` 隔离，正式包不含这两个入口。

`ReferenceStills/` 仅保留少量历史评审参考。旧版 `DraculasBox/Prototypes/Showcase/` 数据与 Unity 菜单中的 Prototypes 预览窗口已移除；正式作品演示以本目录中的 `Pages.json`、`Clips/` 和主菜单「作品演示！」为准。

## 正文强调

每页 `guide` 保持纯文本，用 `\n\n` 分段。`highlights` 单独列出需强调的原文词句，例如 `"highlights": ["两格阴影", "通路与光路"]`。渲染时跟随当前主题强调色，保持原字号与字重；不必手写富文本标签。修改正文时同步更新关键词，通常每页 1–2 处即可。
