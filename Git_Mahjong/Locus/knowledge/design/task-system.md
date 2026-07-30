---
id: kd_92c57482-a934-48e5-85b7-7d33f9d5e1ad
type: design
path: task-system.md
title: task-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785403317874
updatedAt: 1785407050693
---

# task-system

## Summary
已确认的本地任务系统：场景 TaskManager 负责事件驱动进度和存档，TaskPanel 仅展示两类任务。

## Content
# 任务系统

- `TaskManager` 为场景 `Manager` 节点的单例组件，负责任务配置、进度、领奖、每日刷新与 PlayerPrefs JSON 持久化。
- 任务配置直接写在 `TaskManager.cs`，分为 `dailyTasks` 与 `mainlineTasks` 两组列表；任务描述保存语言键，并由 `TaskItem` 使用 `string.Format(LanguageManager.Instance.GetText(key), targetProgress)` 格式化。英文文本配置位于 `EnglishLanguageConfig`。
- 当前时间统一调用 `GameManager.GetNowTime()`；每日任务依据 `yyyyMMdd` 日切重置，主线任务永久保留。
- 每日登录由 `GameManager.Init()` 最后触发 `GameEvent.DailyLogin`；`TaskManager` 收到后比较保存日期和当前 `yyyyMMdd`，仅跨天时重置每日任务并增加登录进度。
- `TaskManager` 通过 `IEventListener` 监听 `GameEvent.MahjongGameWon`、`GameEvent.DailyLogin` 和 `GameEvent.PlayAds` 并更新对应任务；广告任务进度类型为 `TaskProgressType.PlayAds`。
- 主线任务共 20 条：10 条累计通关里程碑和 10 条关卡进度里程碑，目标均为 5、10、15、20、30、40、50、70、100、150。
- 关卡进度使用 `GameManager.Instance.playerInfo.level` 作为当前进度源；读取主线任务列表时同步为 `min(level, targetProgress)`，无需额外事件累加。
- 配置更新时按任务 ID 保留已有进度和领取状态。
- `TaskPanel : UIBase` 仅负责每日/主线 Tab、滚动列表和任务状态展示；单条 `TaskItem` 负责将领取操作转发给 `TaskManager`。
- 入口位于 `GameScenePanel` 右上角，打开 `TaskPanel`。
- 任务奖励当前使用玩家金币；广告任务要求广告 SDK 在确认激励完成后触发 `EventManager.Instance.TriggerEvent(GameEvent.PlayAds)`。
