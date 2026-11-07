# Box or Void 学习复刻：工程事实基线

检查日期：2026-11-06。初始提交：`8f075f8`。当前状态见 README、Review 和 Evidence/delivery-audit.json；未提交、推送或发布。

## 当前工程事实

- 入口 `Assets/Studies/BoxOrVoid/Scenes/BoxOrVoid.unity`，21 个 JSON 关卡共用一个场景；Core、Runtime、Editor 分程序集。
- 后续交互迭代：共享场景现已保存 Menu Canvas/Main Menu 的真实 UI 和 Transition Canvas；PuzzleApp 使用序列化的 StudyMenu/StudyTransition 引用。标题页不再在运行时重建，其他数据页面继续按需生成。圆形遮罩按 0.3 秒闭合/0.3 秒展开，在完全遮盖帧后切换数据与页面。
- 游戏内编辑器仅完成待审核 TD 方案及 HTML 交互稿，入口为 `Design/InGameEditor/`；当前 Unity LevelWorkbench 仍是研究 JSON 辅助工具，不能把它记成完整游戏内编辑器。
- 实际 Editor 为 Unity 6000.3.6f1；已加载 URP 17.3.0、Test Framework 1.6.0、TMP 5.0.0、UGUI 2.0.0。开工时 manifest 的旧版本声明不能代表当前解析版本。本次没有升级 Packages，也没有删除 Library 做干净检出验证。
- Windows x64 / Mono 开发构建以 Direct3D11 在 RTX 3060 Laptop GPU 上运行；只包含研究场景，使用模块自己的 2D 渲染管线。
- 已连接正确 Unity MCP 工程实例；通过内部接口及独立后台 Player 验证，没有接管系统鼠标键盘。
- 52 项 EditMode 检查通过；最终 Windows 回放与移动中撤销检查见 WindowsRun03。工作台、参考视频状态对照、逐关 LD 分析已经形成独立记录。
- 本次 UI 迭代再次运行 52 项 EditMode 与全部 21 关 Windows 回放，最新证据在 `Evidence/Presentation/WindowsRun04/`；WindowsRun03 保留为上一轮历史证据。
- 初次构建触发的 URP / SpriteAtlas / ProjectSettings 自动序列化差异已归档并恢复。后续构建助手在作用域内保存/恢复；宿主产品名仍为 Pro0，原 Build Settings 入口保留。
- 初始用户改动 Renderer2D.asset、Renderer3D.asset、Investigate/ 不属于本任务；最终 Git 检查应只增加 Assets/Studies* 和 Studies/。

## 以下为开工时历史基线

下列“尚未”“拟”“未运行”等表述记录开工时事实，不代表当前交付状态。

## 任务入口

- [GOAL.md](GOAL.md)：待用户在本会话设置的目标提示词；本轮未创建或启动平台 Goal。
- 用户最新范围：截图中「欢迎」「箱即是空」两章，质量优先。后续实施限于此范围。
- Unity 资产拟隔离于 `Assets/Studies/BoxOrVoid/`；学习复刻文档、研究工具和产物归档于本目录。尚未创建玩法资产。

## 已确认的工程情况

| 项 | 文件检查结果 |
|---|---|
| 工程 | `G:\Unity\projects\My\Pros\TD\SokobanGame` |
| Unity | `6000.3.6f1`，revision `bbb010bdb8a3` |
| 渲染 | manifest 声明 URP `12.1.15`；GraphicsSettings 引用 `Assets/Settings/Graphics/URP_Pipeline.asset` |
| 输入 | PlayerSettings 中 `activeInputHandler: 0`，即旧 Input Manager；尚未验证具体按键绑定 |
| 测试 | manifest 声明 Unity Test Framework `1.1.33`；未运行测试 |
| 其他依赖 | UniTask 为本地包，TextMeshPro `3.0.6`，继承 CakePac、DOTween、Aya.Events 等；不代表已验证兼容 |
| 当前构建场景 | `Assets/Scenes/Gameplay.unity`；仅依据 EditorBuildSettings，未认定为本次复刻入口 |
| 现有代码 | `Assets/Scripts/CakePac/` 为继承工具框架；抽查的 `UIManager` 继承单例基类，尚无玩法实现；未全面审计代码 |
| Unity MCP | manifest 与 lock 有 `com.coplaydev.unity-mcp`；本会话工具清单未发现 Unity MCP 操作工具，不能据安装声明认定已经连接 |

查看到正在运行的 Unity 进程，但尚未核对它打开的工程或场景。实施时必须重新确认目标实例。工程的 Unity 与依赖兼容性尚未核验。

Git 检查存在以下先前改动：`Assets/Settings/Graphics/Renderer2D.asset`、`Renderer3D.asset` 已修改，`Investigate/` 未跟踪。不得将这些改动归因于本任务或覆盖；继续实施前重新检查。

## 本地参考资料

- 游戏根目录：`D:\Steam\steamapps\common\Box or Void Demo`。
- 存在 Mono 目录与 `Box or Void_Data/Managed/Assembly-CSharp.dll`（727552 字节）；尚未反编译，不据文件名推定具体玩法代码。
- `Box or Void_Data/StreamingAssets/Levels/` 共 42 份 JSON：Tut 8、Box 13、Snake 11、Sticky 10。本次仅前两个目录的 21 份；须进一步核对游戏内名称、顺序和可达性。
- 抽查 `Box/n_box_1.json` 存在 width、height、grid、Objects、ObjectType、Color、allowUp/Right/Down/Left 字段。字段的完整语义、枚举映射及坐标约定尚未确定。
- 视频文件存在：`F:\GDocument\Eagle\实况资源库.library\images\MNU375V1N2HI1.info\Box or Void 箱即是空_Demo.mkv`，大小 280112879 字节。本轮未重新观看视频或试玩原作。

## 项目约定与验证边界

已读取根 `Agent.md` 及其指定的版本路线、设计基准、决策日志、技术框架。现有文档建立的是初始化基线，不能把“玩法待定”视为用户尚未授权本次学习复刻。

- 保留无关用户改动，维护序列化资源身份；提交与推送需要用户另行指示。
- 使用既有 ConoBehavior 时遵循其生命周期编排；不要求本次独立玩法强行依赖全部模板框架。
- 文档、静态检查、编译、运行、自测和用户验收分别记录。
- 现有正式项目文档不应混入全部复刻细节；用户明确要求为复刻单独管理文件。本目录承担本次任务的专属记录，必要时只向根导航添加入口。

本文件只记录已经查看的证据及未知项。实现开始后按实际状态更新，不以此基线代替当前检查。
