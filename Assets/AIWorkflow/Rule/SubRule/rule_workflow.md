# 工作流细节规则

## 需求处理流程
1. 任务进入 `Memory/current_task.json`，填写 goal 和 context_summary
2. `Planner` 快速澄清并判断是否需要 `Architect`
3. `Programmer` 实现改动
4. 快速自检后交付，更新 current_task.json 状态为 done

## 状态转换（4 状态）

```
Planning → Implementation → Delivery
    └→ ArchitectReview（按需）→ Implementation
```

- 默认按最短路径进入下一阶段
- 发现阻塞或重大风险时回到 Planning 重规划
- 里程碑完成时更新 `Memory/ProjectSnapshot.md`
- 只有值得复用的经验才写入 `Memory/knowledge.md`
