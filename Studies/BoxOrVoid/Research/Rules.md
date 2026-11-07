# 前两章规则记录

证据范围：本地 Demo 的 21 份关卡 JSON、程序集反编译、提供的实机录像。独立实现不加载原游戏程序集。以下实现检查通过不等于逐条原作运行对拍完成。

|规则|实现语义|原作依据|当前证据|
|---|---|---|---|
|控制切换|白、黑两个角色切换；对象颜色不互换|GameManager.SwitchActivePlayer；视频 01:30–02:00|教程画面、纯逻辑检查|
|最高层|目标格只以最上层对象判通行；异色对象成为可站立的表面|PlayerController / Box 的碰撞与 sortingOrder 判定|箱链/叠层的独立规则用例|
|墙与空格|同色墙和灰墙阻挡；异色墙、异色物体和空格可进入|PlayerController.TryMove 路径|独立规则用例；空格不是自动死亡|
|推动|同色箱链依次推动，链尾无法进入则整次失败；异色箱留在原格|Box.TryPush / PlayerController|同色箱链原子性、异色支撑用例|
|被覆盖角色|可切换选择，不能自行移动|PlayerController 的最高排序检查|规则用例；欢迎/02 对照；箱子/08 三层实际标记检查|
|目标|上方恰好一个物体，类型与颜色均匹配|Goals / UnderIndicator|叠两物体不能满足目标的用例|
|撤销|移动、推动、颜色切换进入历史；受阻不进入|GameManager 快照与 Undo|13 个核心用例；21 关 Windows 回放与整段撤销|
|重置撤销|重置后可撤回至重置前状态，并继续撤回更早操作；第二次重置替换恢复点|GameManager.CaptureUndoResetCheckpoint / PerformResetWithInputLock|普通与连续重置用例；Windows 重置恢复检查|
|输入节奏|移动动画 0.1 秒、重复间隔 0.12 秒；撤销长按延迟 0.5 秒、之后 0.2 秒|原作 PlayerController 序列化值、GameManager|生产绑定/重复层测试；真实硬件输入另列|
|动画中切换|角色移动期间忽略切换；动画不改变权威状态|GameManager.SwitchActivePlayer 开头检查|输入适配层用例|
|动画中撤销/重置|等待移动停止和两个稳定帧再提交；最后请求覆盖待处理请求|GameManager.CommitQueuedUndoResetEndOfFrame|队列用例；WindowsRun03 的 midMoveUndoPassed|
|叠层提示|目标完成勾优先，否则只显示顶层的直接下层对象|实际使用的 UnderIndicator|三层格仅一个黑角色标记；移动后 0.3 秒显现|

## 已发现并修正的表现问题

- 原作 Sprite 的图像内容可能被裁切。UnityPy 默认导出裁切块；不能按裁切图宽高归一成一格。已恢复 m_Rect 全画布、textureRectOffset、m_Pivot 和 m_PixelsToUnits，保留墙角、目标和上层提示的实际位置。
- 关卡编号不是文件序号：箱即是空 /03 来自 n_box_13.json；后续包含 A/B/C 分支。以 Catalog.json 的显示编号和解锁关系为准。
- 后台编辑器初次 Play 仅推进一帧；对旧图像和动画的判断因此无效。已为隔离测试会话启用后台帧更新，退出时恢复设置。正常输入仍服从焦点条件。

## 对照结果与边界

- 欢迎/02 六个视频状态已重建，详见 ReferenceComparison.md；连接状态的输入由观察推断，不是原视频键盘日志。
- 完成使用约 1 秒的延迟/消散/返回流程，真实 Player 流程检查通过并有实际混音。粒子轨迹、音乐包络、部分菜单与转场不是逐帧复刻。
- 系统硬件键盘和鼠标未接管验证；虚拟按键经过相同绑定/重复/分发层。手柄未实现。
