---
id: kd_1ca41451-a228-4ba7-af9b-bcf2e10d0d3a
type: memory
path: unity-project-understanding/mahjong-gameplay.md
title: mahjong-gameplay
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785138573722
updatedAt: 1785297052615
---

# mahjong-gameplay

## Summary
Mahjong 主玩法代码结构与当前实现进度缓存。

<!-- locus:body:start -->
- 玩法设计见 `design/mahjong-core-gameplay.md`。
- 阶段1 Model 位于 `Assets/scripts/Mahjong/Model/MahjongModels.cs`，命名空间为 `MahjongGame.Model`。
- 当前包含 MahjongCardState、MahjongGameState、MahjongGridPosition、MahjongCardModel、MahjongSlotModel、MahjongGameModel、MahjongConfig。
- Model 未引用 UnityEngine、UGUI、现有 UI 框架或场景类型。
- MahjongConfig 仅保留全局固定规则：2张消除、4槽容量、逻辑坐标步长和关卡目录资源路径。
- 阶段2位于 `Assets/scripts/Mahjong/GameLogic/`，命名空间为 `MahjongGame.GameLogic`。
- `MahjongLayoutGenerator` 从固定关卡配置创建类型、层级与坐标；`MahjongBoardRules` 处理遮挡、同层同行左右紧邻及选择校验；`MahjongSlotRules` 处理入槽与2张消除；`MahjongGameLogic` 是 View 可调用的唯一主业务入口。`MahjongLayoutGeometry` 为运行时、求解器与编辑器共享半格几何规则：偶数层不偏移，奇数层横纵各偏移半格，任一更高层牌与目标牌矩形有面积重叠即遮挡。
- 运行时关卡定义位于 `Assets/scripts/Mahjong/Model/MahjongLevelModels.cs`，每关仅配置网格行列和每张牌的固定类型、层级、坐标；层数、牌型和数量从 cards 自动推导。
- `Assets/Resources/Mahjong/Levels.json` 是单一关卡目录，使用整数网格坐标：偶数层 column/row 范围为 `0..count-1`，奇数层允许左侧与底部扩展带，最小值为 `-1`；布局可在网格内留白但保持紧凑，并分散同类型牌。
- `MahjongLevelCatalogLoader` 加载并缓存关卡，按 `GameManager.Instance.playerInfo.level` 取关，缺失等级时按随机种子随机选择已有关卡。
- 运行时 MahjongLevelCatalogLoader 和 MahjongLayoutGenerator 只反序列化、查询并创建卡牌，不校验关卡版本、结构、边界、数量、重复位置或可解性。
- `MahjongLevelValidator` 通过 `UNITY_EDITOR` 限制为编辑器校验代码；`Assets/scripts/Mahjong/Editor/MahjongLevelSolver.cs` 与 `MahjongLevelCatalogEditorValidator.cs` 在导入/生成配置或执行 Tools/Mahjong/Validate Level Catalog 时完成全部结构与可解性校验。
- `MahjongOperationResult` 返回成功状态、失败原因、移动/消除卡牌ID和结算状态；洗牌、撤销当前返回 FeatureNotImplemented 且不修改数据。
- 阶段2未引用 UnityEngine、UGUI 或现有 UI 框架。
- 阶段3 View 位于 `Assets/scripts/Mahjong/View/MahjongCell.cs`，预制体位于 `Assets/Prefab/Mahjong/MahjongCell.prefab`；卡牌尺寸为 171×199，Background 子 Image 显示背景，Icon 子 Image 显示类型图片，TypeText 仅在图片缺失时显示数字占位，并由 CanvasGroup 统一控制交互与透明度。MahjongCellBackgroundStyle 和 SetBackgroundStyle 提供默认、绿、黄、红背景切换接口，预制体分别绑定 `Assets/Texture/Mahjong/bg/di*.png`；当前玩法不主动调用该接口。
- MahjongCell 实现 IBeginDragHandler、IDragHandler、IEndDragHandler、IPointerClickHandler，仅派发选择意图，不修改 Model；拖拽入槽使用卡牌中心相对 SlotRoot 世界矩形的归一化插值判定。
- `Assets/Resources/UI/Panel/GameScenePanel.prefab` 的 `Root/MahjongGameplay` 独立节点挂载 `MahjongGameplayView`，其子节点为 BoardRoot、SlotRoot、DragLayer，并绑定 MahjongCell 预制体。
- `Assets/scripts/Mahjong/View/MahjongGameplayView.cs` 持有唯一 MahjongGameLogic，按 MahjongOperationResult 驱动 DOTween 入槽、消除、回位和阻挡刷新。
- MahjongGameplayView 内置专用 `Stack<MahjongCell>` 对象池；重新开局和配对消除均回收卡牌视图，构建新局时优先复用，不引入独立对象池框架。
- GameScenePanel 仅保留 UIBase 生命周期转发、MahjongGameplayView 引用、lobbyEnter 和大厅导航，不再包含麻将玩法实现。
- 阶段3运行验证覆盖动态卡牌数量生成、卡槽外拖拽回位、点击入槽、拖拽入槽和2张配对消除。
- `Assets/Resources/Mahjong/MahjongCardVisualCatalog.asset` 统一配置 typeId 到 Sprite 的映射，由 `MahjongCardVisualCatalogLoader` 供运行时 MahjongCell 与关卡编辑器 Card Palette 共用；缺少图片时恢复颜色底图和数字 ID 占位。
<!-- locus:body:end -->
