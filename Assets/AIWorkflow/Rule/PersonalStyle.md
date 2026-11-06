# 个人风格与偏好

> 本文件记录个人的代码规范、设计倾向和经验偏好，跨项目通用。开始新任务前 AI 应阅读此文件，确保产出风格一致。

---

## 代码规范
### 结构组织
- 使用 `#region` + 中文标题整理脚本结构，按功能模块划分
- 扩展方法用于工具函数（集中在 `ExpandedClass.cs`）

### 代码精简
- 尽量减少代码行数和函数冗余
- 避免过度封装，保持代码简洁直观

### 注释原则
- 对不易懂的逻辑保留必要注释
- 删除无意义的注释（如自解释的代码）
- 注释使用中文，简洁明了

### 命名规范
- 公共成员使用 PascalCase（`PlayerHealth`）
- 私有成员使用 camelCase（`playerHealth`）
- 常量使用 PascalCase（`MaxHealth`）
- 布尔变量使用 `b/Is/Has/Can` 前缀（`IsDead`、`CanJump`）
- 协程方法使用 `~` 后缀（`WaitForSeconds` → `WaitForSeconds~`）

### 异步与性能
- 异步任务统一使用 UniTask，不混用 Coroutine 和 `Task.Delay`
- 高频路径（Update、碰撞检测）必须零 GC 分配
- 避免在 Update 中每帧调用 `Physics.OverlapSphere`，改用事件触发或降频检测

---


## 架构倾向

- **继承优先于组合**（在 Unity 组件体系内）：用 `ConoBehavior` 基类统一生命周期，`Triggerable → Interactable → 具体交互物` 这类继承链是首选
- **单例只用于全局管理器**：`Singleton<T>` 适用于 `World`、`GenericUIManager` 等；业务组件不用单例
- **事件驱动跨模块通信**：模块间通信优先 UEvent，避免直接持有跨模块引用
- **接口抽象系统边界**：可受伤物体用 `IDamageable`，类似的能力边界用接口定义
- **不过早抽象**：三处相似代码才值得提取；一次性逻辑不包装成工具类

---

## 设计倾向

- **最小可玩优先**：新功能先做能跑起来的最简版本，再迭代完善，不设计"完美初版"
- **关卡设计友好**：脚本参数尽量暴露 SerializeField，配合 Odin Inspector 提供清晰的编辑器体验
- **手感>功能完整性**：移动/攻击/交互的手感优先于功能数量
- **视觉反馈不省**：任何用户操作都要有即时反馈（DOTween 动效、粒子、音效），哪怕 placeholder

---

## 经验偏好

- **调试友好**：Gizmos 可视化调试区域；`[ContextMenu]` 提供编辑器测试快捷入口
- **异步一致性**：整个项目统一 UniTask，不因为"这里只是一个等待"就随手写 Coroutine
