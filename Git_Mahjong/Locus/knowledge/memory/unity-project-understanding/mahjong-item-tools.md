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
updatedAt: 1785399480745
---

# mahjong-item-tools

## Summary
Mahjong 撤销、洗牌与提示道具的运行时实现与入口缓存。

<!-- locus:body:start -->
- `MahjongGameLogic` 现已实现 `Undo()`、`Shuffle()` 与 `GetHintCardIds()`：撤销仅移出卡槽末尾的稳定牌并恢复为 `OnBoard`；洗牌只让 `OnBoard` 牌随机交换完整 `Layer + Position`；提示优先返回“可选牌面牌 + 同类型卡槽牌”，不存在该组合时才返回两张可选同类型牌面牌；相同优先级下会按候选顺序循环，避免连续重复上一组。
- `MahjongCardModel.Layer` 与 `Position` 已改为私有可写，并通过 `SwapBoardPosition` 保持位置交换封装在 Model 内。
- `MahjongGameplayView` 对外提供 `TryUndo()`、`TryShuffle()`、`TryShowHint()`、`StopHint()`；所有道具操作均要求没有入槽或消除动画。洗牌后更新 UGUI 坐标、显示层级与阻挡状态；撤销后将末尾槽牌重新放回 BoardRoot。
- `MahjongCell` 在 Awake 查找既有 `HintEffect` 子节点，并由 `SetHintEffectActive` 开关；预制体路径为 `Assets/Prefab/Mahjong/MahjongCell.prefab`，该节点自带 Animation `HintEffectAnim`。
- `GameSceneItem_Return`、`GameSceneItem_Exchange`、`GameSceneItem_Hint` 通过 `GameScenePanel` 的公开麻将道具接口调用玩法逻辑；仅成功时扣除道具。`GameScenePanel` 监听 `GameEvent.StopHintAnim` 并关闭所有提示特效。
<!-- locus:body:end -->
