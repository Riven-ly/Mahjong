---
id: kd_935405f6-c8d2-4de8-937c-6d6c7073a6b4
type: memory
path: unity-project-understanding/mahjong-selection-pairing.md
title: mahjong-selection-pairing
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1786171762201
updatedAt: 1786179753066
---

# mahjong-selection-pairing

## Summary
麻将双点击配对玩法的运行结构缓存。

<!-- locus:body:start -->
- `Assets/scripts/Mahjong/GameLogic/MahjongGameLogic.cs` 的 `MarkPairForElimination` 校验两张牌均可选且类型相同，随后置为 `PendingElimination`；`LoseGame` 由生命耗尽调用。
- `Assets/scripts/Mahjong/View/MahjongGameplayView.cs` 用 `selectedCardId` 管理第一次点击；不同类型时双牌抖动并经 `HealthChanged` 扣血。每关初始生命为 `MahjongViewConfig.InitialHealth=3`。
- 消除中心为两张牌当前世界坐标中点。成功配对后两张牌均显示 HintEffect。两牌先分别前往该中心的局部 X±`MahjongViewConfig.TransitOffsetX`（300）中转点，再移动到以卡牌边缘接触为止的首次碰撞点，向外回弹 60，最后再次接触；第二次接触时在中点播放池化粒子并缩放消失。
- 两张消除牌会先保持世界坐标转移至 `GameScenePanel/Root/MahjongGameplay/EliminationLayer`，该层为 MahjongGameplay 最后同级节点，确保消除动画覆盖其他牌。
- `Assets/Resources/UI/Panel/GameScenePanel.prefab` 已删除 SlotRootBg、SlotRoot、DragLayer；GameSceneItem_Return 已恢复显示。
- 运行时消除粒子使用 `Assets/Prefab/Mahjong/MahjongEliminationFragments.prefab`（不是旧记忆中的 Fragments2）。该 Prefab 现为两层 Mesh Cube 粒子：根层 `MahjongEliminationFragments` 在 0/0.09/0.19 秒分三波发射白色麻将碎片，`AccentFragments` 在 0.04/0.14 秒补发少量红绿碎屑；均向左右扩散、先上扬后受重力下落并缩小淡出。`GameScenePanel/Root/MahjongGameplay/EliminationFloor` 是不可见的 Plane Collision 平面；`MahjongGameplayView.ConfigureEliminationEffectCollision` 在初始化与池扩容时为所有根/子粒子绑定该平面，设置 0.45 回弹、0.35 阻尼和 0.2 生命周期损失，以试验碎片落地回弹。
- `GameSceneItem_Return` 通过 `GameScenePanel.TryAutoEliminateMahjongPairs` 调用 `MahjongGameplayView.TryAutoEliminate(5)`，成功启动才消耗一个道具。`MahjongGameplayView` 的 `autoEliminationRemainingGroupCount` 在 `CompleteEliminationAnimation` 中递减并接续下一组；无可操作配对时保留牌面调用 `MahjongGameLogic.Shuffle` 后继续查找。
- `MahjongGameplayView.EnsurePlayablePair` 在开局、手动洗牌与每次消除结算后检查 `MahjongGameLogic.GetHintCardIds`；没有当前可操作同类型配对时，在逻辑层保留未消除牌并后台连续洗牌，找到一组配对后才一次性刷新牌面。`MahjongLevelRandomizer` 已沿可取顺序写入连续牌对，常规局面不应频繁触发该兜底。
<!-- locus:body:end -->
