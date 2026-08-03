---
id: kd_3e9a57a8-a19e-48f5-814d-f81c127f80d6
type: memory
path: unity-project-understanding/mahjong-item-tools.md
title: mahjong-item-tools
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785398340269
updatedAt: 1785480635704
---

# mahjong-item-tools

## Summary
Mahjong 撤销、洗牌与提示道具的运行时实现与入口缓存。

<!-- locus:body:start -->
- `MahjongGameLogic` 现已实现 `Undo()`、`Shuffle()` 与 `GetHintCardIds()`：撤销仅移出卡槽末尾的稳定牌并恢复为 `OnBoard`；洗牌只让 `OnBoard` 牌随机交换完整 `Layer + Position`；提示候选顺序为所有“可选牌面牌 + 同类型卡槽牌”，再接所有“可选同类型牌面对”，并循环轮换。
- `MahjongCardModel.Layer` 与 `Position` 已改为私有可写，并通过 `SwapBoardPosition` 保持位置交换封装在 Model 内。
- `MahjongGameplayView` 对外提供 `TryUndo()`、`TryShuffle()`、`TryShowHint()`、`StopHint()`；所有道具操作均要求没有入槽或消除动画。洗牌后更新 UGUI 坐标、显示层级与阻挡状态；撤销时卡牌以保持世界位置的方式切回 `BoardRoot`，再按 `ReturnDuration` 补间回原网格位置；动画期间通过 `movingCardIds` 锁定道具与牌面输入，完成后依层级重排全部牌面视图。重复撤销同一张牌前需先 `DOTween.Kill(this)` 清除该视图残留补间，再开始新的回位动画。不可仅将撤销牌 `SetAsLastSibling()`，否则洗牌后会让低层撤销牌覆盖高层牌，造成显示与遮挡规则不一致。
- `MahjongCell` 在 Awake 查找既有 `HintEffect` 子节点，并由 `SetHintEffectActive` 开关；预制体路径为 `Assets/Prefab/Mahjong/MahjongCell.prefab`，该节点自带 Animation `HintEffectAnim`。
- `GameSceneItem_Return`、`GameSceneItem_Exchange`、`GameSceneItem_Hint` 通过 `GameScenePanel` 的公开麻将道具接口调用玩法逻辑；仅成功时扣除道具。`GameScenePanel` 监听 `GameEvent.StopHintAnim` 并关闭所有提示特效。
<!-- locus:body:end -->
