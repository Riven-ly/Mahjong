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
updatedAt: 1786074689790
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

## 提现任务

- 提现面板为 `TxPanel`，显示 `PlayerInfo.Gold` 现金余额、四个固定金额按钮（$100、$200、$500、$1000）和所选档位的阶段进度；Change 与 Withdraw Cash 不属于此功能范围。
- 四个档位都是相互独立、并行推进的三阶段任务链：先达到对应现金余额，再累计通关，最后累计签到。$100 任务依次为达到$100、通关15关、签到7天；$200 为达到$200、通关30关、签到10天；$500 为达到$500、通关75关、签到15天；$1000 为达到$1000、通关150关、签到20天。
- 余额阶段以 `PlayerInfo.Gold` 判断，首次达到对应金额即永久完成并保存，后续金币消费不会使该阶段回退。
- 每个档位的通关计数互相独立；同一次 `GameEvent.MahjongGameWon` 只推进已完成余额阶段、尚未达到通关目标的档位。
- 签到进度直接读取 `TaskManager.saveData.loginDays`，不另建签到计数；仅在余额和通关阶段均完成后显示签到阶段。
