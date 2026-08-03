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
updatedAt: 1785478508121
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
- `Assets/Scenes/Game.unity/Manager/TaskManager` 是场景级任务单例，监听 `MahjongGameWon` 与 `RewardedAdCompleted`，状态以 `TaskManagerData` JSON 独立存入 PlayerPrefs。
<!-- locus:body:end -->
