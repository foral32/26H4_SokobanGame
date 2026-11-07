# Box or Void 前两章学习复刻

范围为「欢迎」8 关、「箱即是空」13 关。所有关卡从 JSON 加载，共用一个 Unity 场景。

这是当前工程中的独立学习模块，提供标题、两章选关、游玩、暂停、音量设置、完成解锁与独立进度，以及完整的游戏内关卡编辑器。编辑器的实现与验证见 [实施记录](Design/InGameEditor/实施记录.md)，基础复刻的质量判断和剩余差异见 [Review.md](Review.md)。所有者体验验收单独进行。

2026-11-06 交互迭代：已加入圆形遮罩转场、按钮按压/选择反馈和面板入场。主菜单的背景、标题、按钮与选择指示器已保存为共享场景中的真实对象，停止运行也可查看和修改。

2026-11-07：所有者确认玩家界面方向后，已将「关卡创作」接入 Unity。中央棋盘、底部物件栏、圆形按钮、选关式卡片与按需展开的格子小窗构成主界面，完整工具保留在第二层菜单。根据文字设计反馈，标题、正文和操作提示分级排版；采用「灰墙」「单击放置 · 拖动绘制」等简洁文案。见 [设计与实机入口](Design/InGameEditor/README.md)。

## 直接游玩 Windows 版

若要研究开发方法，可打开独立的 [TD 交互拆解工作台](Atlas/README.md)：规则沙盘、程序结构、21 关设计表与回放、双关对照、美学交互、音效试听和阅读笔记。入口为 `Atlas/打开拆解工作台.cmd`，当前地址 `http://127.0.0.1:4177/`。

打开 `Studies/BoxOrVoid/Builds/Windows/`，双击 **开始游戏.cmd**，以 1280×720 可调整窗口启动。也可以直接运行 `BoxOrVoidStudy.exe`；需保留整个目录及相邻的 `_Data`、DLL 文件。

这是 Unity 6000.3.6f1 的 Windows x64 / Mono 开发构建。首次从「欢迎/01」开始，按原作顺序解锁。没有预先写满真实进度。

## 在 Unity 中运行

1. 使用项目现有 Unity 6000.3.6f1。
2. 打开 `Assets/Studies/BoxOrVoid/Scenes/BoxOrVoid.unity`，进入 Play Mode。
3. 从标题「开始游戏」进入章节和选关，首次仅开放「欢迎/01」；「关卡创作」直接进入自己的关卡库。

场景固定界面位于 `Box or Void Study / Menu Canvas / Main Menu`，转场位于同级 `Transition Canvas`。这些就是运行时复用的对象，不是临时编辑器预览。关卡棋盘与选关目录继续按数据生成。`Author scene presentation` 只用于旧场景的一次迁移；已生成的主界面直接在层级中调整，不需重复执行准备场景命令。

|操作|按键|
|---|---|
|移动 / 菜单选择|方向键、WASD|
|切换控制颜色|空格|
|菜单确认|空格、Enter|
|撤回|Z，可长按|
|重置关卡|R；重置也可撤回|
|暂停 / 返回|Esc|

菜单提供鼠标指向与点击路径；暂停页显示操作说明。设置页用左右键调整音乐和音效。验证使用隔离虚拟按键，经由相同的运行时轮询、绑定、重复和操作分发路径；没有接管系统鼠标键盘，不能据此宣称真实硬件操作已验收。未实现手柄和按键重映射。

## 游戏内关卡编辑器

从主菜单进入「关卡创作」：新建空白棋盘或内置 21 关的独立副本，编辑后按 F5 试玩，再按 F5 / Esc 返回编辑；布局、视角与编辑历史保留。保存允许未完成草稿，试玩前的错误提示可以直接引导放置缺少的角色或定位有问题的格子。

支持连续画笔、按物件擦除、查看并编辑遮挡层、取物件、直线、空心/实心矩形、地形填充、追加框选、复制/移动/旋转/镜像、尺寸锚定与裁切预览。一次绘制或变换对应一次撤回。裁切需要确认，可撤回恢复全部物件。

| 编辑操作 | 快捷键 |
|---|---|
| 撤回 / 重做 | Z / Y，也支持 Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z |
| 选择物件 / 切换黑白 | 1–6 / Tab |
| 摆放 / 查看 / 擦除 | B / V / E |
| 取物件 / 直线 / 矩形 / 填充 | I / L / U / F |
| 框选 / 追加框选 | M / Shift+拖动 |
| 复制 / 移动 / 粘贴 / 全选 | Ctrl+C / Ctrl+X / Ctrl+V / Ctrl+A |
| 删除选区或选中层 | Delete / Backspace |
| 缩放 / 平移 | 滚轮 / 中键或空格拖动 |
| 黑白显示 / 网格 / 适配 | Q / G / Home |
| 试玩并返回 / 保存 / 操作指南 | F5 / Ctrl+S / F1 |

Esc 按层取消弹窗、粘贴、绘制、格子查看和选区，再返回关卡库。文本框聚焦时编辑快捷键暂停。按钮上的键位和 F1 指南均在游戏内可见。

「我的关卡」可重新打开、复制、改名、回收与恢复。导出可选择文件夹，重名自动追加序号；点「导入」打开 Windows 文件选择器，或在关卡库按 Ctrl+V 粘贴 JSON 路径。确认预览后创建独立副本。支持本编辑器文档与原有布局 JSON，不覆盖内置关卡或已有用户关卡。自动草稿、上次保存备份与手动保存分开管理；“已玩通”绑定实际完成的布局与规则版本，布局修改后失效。

固定 UI 保存在 `Box or Void Study / Level Editor Canvas / Player UI`。Canvas 在标题页默认隐藏，停止运行时可在层级中临时启用查看；运行时使用同一批对象。内容和关卡仍是数据，不为每关建立 Scene。

## Unity 内的数据辅助工作台

旧工作台保留给开发者直接检查 JSON。玩家使用上述游戏内编辑器即可完成闭环。

打开 `Tools > Box or Void Study > Level data workbench`，选择关卡 JSON，查看每格从底到顶的对象，添加或删除层，检查数据。

- 原始数据：`Assets/Studies/BoxOrVoid/Content/Levels/`。
- 工作台保存到 `Content/Drafts/`，保留原始 21 关。
- 每次载入后第一次保存采用新文件名，避免覆盖已有副本；同一次编辑后续保存更新该副本。`custom_study.json` 是实际试跑留下的示例，不加入正式选关。
- 在共享场景 Play Mode 中点击试跑，直接替换运行时关卡数据，不生成场景。
- 「数据有效」仅表示结构和对象约束有效，不表示关卡可解。
- 显示编号和文件编号并非一一顺序对应，例如「箱即是空/03」为 `n_box_13.json`。

## 构建与存档

构建入口：`Tools > Box or Void Study > Build Windows study`。明确只构建本研究场景，使用模块自己的 URP 2D Renderer。构建期间临时设置渲染管线、产品名与窗口参数，结束后恢复宿主设置与 Unity 自动改写的序列化文件。输出在 `Studies/BoxOrVoid/Builds/Windows/`，构建报告在 `Evidence/Build/`。

正常进度位于 Unity `Application.persistentDataPath/BoxOrVoidStudy/progress-v1.json`。它保存已完成关卡、上次选择和音量，不写入原游戏存档。不可解析的存档保留为 `.invalid-*` 文件；自动化使用独立临时存档目录。

游戏内自建关卡位于同级 `UserLevels/`：`Levels/` 为正式保存，`Recovery/` 为自动草稿，`Recycle/` 为回收箱，`Shared/` 为默认导出目录。保存使用临时文件与原子替换，保留 `.bak`；已损坏的原文件另行归档。试玩不写剧情进度。

Windows 产品名为 `Box or Void Study`，当前公司名继承 `VoidCake`，常规路径为 `%USERPROFILE%/AppData/LocalLow/VoidCake/Box or Void Study/BoxOrVoidStudy/`。Unity Editor 仍使用宿主产品名 `Pro0`，因此编辑器与 Windows 构建进度分开。

## 结构与证据

- `Assets/Studies/BoxOrVoid/Core/`：不依赖 UnityEngine 的确定性状态、推动、目标和撤销规则。
- `Runtime/`：输入适配、表现、界面流程、音频与存档。
- `Runtime/LevelEditor/`：关卡文档、编辑历史、用户关卡库、玩家 UI 与独立试玩闭环。
- `Editor/`：准备共享入口、JSON 工作台、构建和后台验证工具。
- `Content/Catalog.json`：两章编号、解锁关系、方向导航、表现资源映射。
- `Content/Replays/`：全部 21 关的有效解法，由独立求解器生成，未宣称最短。
- [Research/Rules.md](Research/Rules.md)：规则、来源、已发现问题和待补证项。
- [Research/LevelAnalysis.md](Research/LevelAnalysis.md)：全部 21 关的教学、关键发现、解题依赖与难度衔接；[ReferenceComparison.md](Research/ReferenceComparison.md) 是视频状态对照。
- `Research/AssetSources.json`：本地原作展示素材的来源与校验值。复刻运行逻辑不依赖原作程序集。
- `Evidence/Runtime/`：实际 Unity 截图、代表关按键回放与流程报告。
- `Evidence/Creator/`：本次游戏内编辑器的测试、实际 UI 流程和渲染截图；优先以 [实施记录](Design/InGameEditor/实施记录.md) 中对应的结果为准。
- `Evidence/Presentation/editmode_52.json`：本次迭代的 52 项 EditMode 检查结果。
- `Evidence/Presentation/WindowsRun04/`：最新构建的 21 关实际帧回放、移动中撤销、菜单流程、遮罩闭合换页、重复请求拦截、场景主菜单复用与截图。
- [转场实机短片](Evidence/Presentation/Transitions.mp4)：最新 Windows 构建的标题→章节→选关→关卡过渡；实际相机/音频采集。
- [停止运行时的主菜单](Evidence/Presentation/scene_menu_editmode.png)：Edit Mode 下直接渲染已保存的场景。
- [Windows 实机短片](Evidence/WindowsRun03/Box06_gameplay.mp4)：从最终构建的 Camera 与 AudioListener 实际采集。
- `Evidence/Windows_1024x768/`、`Windows_2560x1080/`：另外两种窗口比例的实际截图及流程检查；窗口布局逻辑与最终版相同。
- `Evidence/Runtime/workbench.json`：修改、非法数据拒绝、撤销、保存副本、单场景试跑检查。
- `Tools/RuleHarness/`：直接使用产品 Core 的独立检查和求解工具。

参考游戏和实机视频保留在用户给定的原位置。反编译、提取中间文件、工具依赖及构建产物在本目录隔离并忽略 Git；未提交或发布。

## 重跑检查

在工程根目录执行 `dotnet run --project Studies/BoxOrVoid/Tools/RuleHarness --no-restore` 检查核心规则。Unity Test Runner 选择 `BoxOrVoid.Tests` 的 EditMode 用例。`python Studies/BoxOrVoid/Tools/verify_delivery.py` 校验来源、共享场景、报告和构建程序集指纹。

开发构建可使用 `--study-validate --study-output <独立证据目录>` 启动隔离回放；追加 `--study-smoke` 仅执行菜单与代表关检查。编辑器闭环使用 `-creator-check -creator-check-output <独立证据目录>`，通过 uGUI 射线命中、指针事件和同一快捷键入口执行。两种模式均使用独立临时存档，完成后自动退出，普通启动不会启用。它们不替代真实键鼠及中文输入法验收。

## 剩余差异

暂停/设置界面为简化文字菜单；省略第三章以后的入口、商店/反馈入口和部分装饰路径。圆形转场现已加入，闭合/展开各 0.3 秒，完全遮盖时换页；暂停与设置面板使用短入场动画。粒子轨迹、部分墙面阴影和音乐切换包络没有逐帧复现。原作美术、音频与关卡素材的来源已留档，玩法逻辑为本模块独立实现。没有宣称逐像素、逐采样或真实硬件验收一致。
