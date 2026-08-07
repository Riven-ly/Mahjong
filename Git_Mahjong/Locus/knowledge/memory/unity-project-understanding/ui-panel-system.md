---
id: kd_187d60c4-9312-41db-9cc4-92b8403eddfa
type: memory
path: unity-project-understanding/ui-panel-system.md
title: ui-panel-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785137072634
updatedAt: 1786074699158
---

# ui-panel-system

## Summary
项目 UI 面板由 UIManager 按类型名从 Resources/UI/Panel 加载，并由 UIBase 分配到场景中的逻辑层。

<!-- locus:body:start -->
- `UIManager.OpenUI<T>()` 使用 `typeof(T).Name`，从 `Resources.Load<GameObject>("UI/Panel/{className}")` 加载预制体。
- 面板脚本继承 `UIBase`；预制体根节点必须挂载同名脚本。
- 通用 Panel 结构参考 `TaskPanel`：根节点含全屏 `Image`、`Animation` 与面板脚本；子节点 `Root` 含 `CanvasGroup`，根节点与 `Root` 均使用全屏拉伸锚点；`uIPanelLayer` 设为 `Layer2`，并配置 `GeneralOpenPanelAnim`、`GeneralHidePanelAnim`。
- `UIBase.uIPanelLayer` 决定实例重设父节点到 Layer1、Layer2、PlayerInfoUI、LobbyScene 或 GameScene。
- `Assets/Scenes/Game.unity` 的 `Manager/UIManager` 已绑定这些 Canvas 子层与 UIMask。
- 场景逻辑页面不是独立 Unity Scene；Lobby/Game 面板分别挂到 Canvas/LobbyScene 与 Canvas/GameScene。
- 当前面板资源位于 `Assets/Resources/UI/Panel/`。
- `GameManager.Init()` 启动时打开 `LobbyScenePanel`。
- `LobbyScenePanel` 与 `GameScenePanel` 使用按钮在两者之间切换，并同步 `GameManager.gameType`。
- 任务面板位于 `Assets/Resources/UI/Panel/TaskPanel.prefab`，通过 `GameScenePanel/Root/TaskBtn` 打开；单条任务预制体为 `Assets/Resources/UI/TaskItem.prefab`。
- `GameScenePanel` 使用 `taskRedImg` 显示任务红点；通过 `TaskManager.TasksChanged` 和面板刷新时检查 `TaskManager.HasClaimableTasks()`，有已完成未领取任务时打开红点。
- `Assets/Scenes/Game.unity` 的 `Manager/TaskManager` 是场景级任务单例，监听 `RewardedAdCompleted` 等任务事件，状态以 `TaskManagerData` JSON 独立存入 PlayerPrefs；`TaskSaveData.loginDays` 记录累计登录天数，每个新日期首次刷新每日任务时增加一次。
- 提现功能使用场景 `Manager` 上的 `TxManager` 单例；状态以 `TxManagerData` JSON 独立保存。每档提现任务保存 `cashReached` 与独立通关计数：首次满足 `PlayerInfo.Gold >= amount` 时永久完成余额阶段，随后才由 `GameEvent.MahjongGameWon` 推进通关阶段，最后读取 `TaskManager.saveData.loginDays` 作为签到阶段。`TxManager` 转发 `TaskManager.TasksChanged`，供 `TxPanel` 刷新阶段。
- `Assets/Resources/UI/Panel/TxPanel.prefab` 为 Layer2 面板；可通过 `UIManager.Instance.OpenUI<TxPanel>()` 按类名加载。
<!-- locus:body:end -->
