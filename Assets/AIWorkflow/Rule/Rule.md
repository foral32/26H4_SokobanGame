# AI Workflow Rule 层（必须先阅读）

**重要：所有 Agent 开始工作前，必须先完整阅读本文件（`Rule.md`）。**

本目录包含适用于**个人、小型 Unity 独立游戏项目**的轻量多 Agent 工作流规则。目标是让 Agent 用最短的安全路径完成任务，而不是引入重型流程。

---

## 新 Agent 冷启动协议（换新 Agent 时按序执行）

> 按此顺序阅读，约 5 分钟可完全上手项目：

1. **本文件**（`Rule.md`）— 工作方式与 Agent 结构
2. **`Project/Memory/ProjectSnapshot.md`** — 项目当前状态、进行中任务、近期关键决策（最重要）
3. **`Project/GameDev/全局策划案.md`** — 游戏是什么、核心循环、里程碑
4. **`Project/GameDev/全局技术文档.md`** — 框架约定、技术栈、性能约束
5. **`Project/Memory/current_task.json`** — 当前任务状态与背景（`context_summary` 字段）

---

## Agent 结构

| Agent | 权限 | 职责 | 激活时机 |
|---|---|---|---|
| **Planner** | 6 | 需求澄清、任务路由、拆解、状态同步 | 每个任务的默认入口，始终激活 |
| **Architect** | 7 | 技术方案、模块边界、跨模块风险 | 仅当 Planner 判断需要架构决策时 |
| **Programmer** | 4 | 代码实现、快速自检、交付 | Planner 或 Architect 完成后 |

角色详情：`Agents/Planner.md`、`Agents/Architect.md`、`Agents/Programmer.md`

---

## 工作流（4 状态）

```
Planning → [ArchitectReview（按需）] → Implementation → Delivery
    ↑                                         |
    └─────────── 发现阻塞，重规划 ─────────────┘
```

**三条执行路径：**
- **简单任务**：Planner（路由）→ Programmer（实现+检查）→ 交付
- **普通任务**：Planner（拆解）→ Programmer（实现+检查）→ 交付
- **复杂任务**：Planner（拆解）→ Architect（方案）→ Programmer（分步实现）→ 交付

任务类型判断规则：`Flow/RequestTypes.md`

---

## 全局原则

- **默认短路**：简单任务不经过架构评审
- **按需升级**：仅在跨模块、公共框架或高不确定性时请求 `Architect`
- **轻量记录**：优先更新 `current_task.json`，不强制生成长文档
- **快速验证**：以自洽性、可执行性和关键路径检查为主
- **经验沉淀**：有复用价值的发现写入 `Memory/knowledge.md`；里程碑完成时更新 `Memory/ProjectSnapshot.md`

---

## 全局文档
- `PersonalStyle.md` — 个人风格：代码规范 + 设计倾向 + 经验偏好
- `TemplateIntro.md` — 模板工程架构说明（CakePac 等通用模块）
- `Config/AgentConfig.json` — Agent 权限与升级配置
- `SubRule/rule_workflow.md` — 工作流细节规则
- `SubRule/rule_planning_loop.md` — 规划循环细节

## AI 工作流

`Assets/AIWorkflow/` 分为两个模块：

- **`Rule/`** — 通用模块，可跨项目复用（Agent 定义、工作流规则、模板架构说明、个人风格偏好）。入口：`Rule/Rule.md`
- **`Project/`** — 项目业务模块（策划案、技术文档、记忆系统、迭代记录）。冷启动快照：`Project/Memory/ProjectSnapshot.md`