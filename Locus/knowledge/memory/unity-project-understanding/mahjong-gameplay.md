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
updatedAt: 1786500991983
---

# mahjong-gameplay

## Summary
Mahjong 主玩法代码结构与当前实现进度缓存；2026-08 已由卡槽玩法切换为双点击配对、三生命与中心汇聚消除。

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
- `Assets/scripts/Mahjong/View/MahjongGameplayView.cs` 持有唯一 MahjongGameLogic，按 MahjongOperationResult 驱动 DOTween 入槽、消除、回位和阻挡刷新；成功选择会立即更新逻辑与牌面可操作状态，并行播放各自的入槽动画，待同组卡牌全部到达卡槽后再播放消除动画。
- MahjongGameplayView 内置专用 `Stack<MahjongCell>` 对象池；GameScenePanel 的 Awake 通过 `GameManager.PrewarmMahjongCells` 按 MahjongLevelCatalogLoader.GetMaximumCardCount() 预创建全关卡目录所需的 MahjongCell，重新开局和配对消除均只回收/复用，主玩法构建阶段不实例化卡牌视图。
- GameScenePanel 仅保留 UIBase 生命周期转发、MahjongGameplayView 引用、lobbyEnter 和大厅导航，不再包含麻将玩法实现。
- 阶段3运行验证覆盖动态卡牌数量生成、卡槽外拖拽回位、点击入槽、拖拽入槽和2张配对消除。
- `Assets/Resources/Mahjong/MahjongCardVisualCatalog.asset` 统一配置 typeId 到 Sprite 的映射，由 `MahjongCardVisualCatalogLoader` 供运行时 MahjongCell 与关卡编辑器 Card Palette 共用；缺少图片时恢复颜色底图和数字 ID 占位。
- 卡槽入槽顺序由 `MahjongSlotRules` 从末尾查找处于 InSlot 状态的相同 TypeId 后插入，`MahjongSlotModel.Insert` 保存该顺序；PendingElimination 牌不会参与后续入槽的同类型插入定位。连续点击允许各新牌并行飞入逻辑计算出的槽位；`MahjongGameplayView.AnimateSlotCellsAfterInsertion` 会将插入点之后的已在槽内 Cell 移向绝对槽位坐标，并通过 `MahjongCell.RetargetToSlotPosition` 中断、重定向仍在飞行的 Cell。每张飞行卡牌由 `slotMoveTweens` 按实例ID管理，`StartSlotMove` 只接受最新补间的完成回调，避免快速点击时旧回调与新目标竞争而重叠。匹配牌进入 `PendingElimination` 状态时仍保留在卡槽并占容量；消除动画完成后由 `MahjongGameLogic.CompleteElimination` 实际移除并标记为 Eliminated。连续消除期间，`CompleteEliminationAnimation` 会在每一组已完成的消除牌从模型和视图移除后立即调用 `LayoutSlotViews`；该方法跳过尚在 PendingElimination 状态的牌，为剩余可保留牌使用连续显示索引，以便即时补位。卡槽满时，`MahjongGameLogic.ResolveGameState` 若存在 PendingElimination 牌则等待消除结算，不会提前判负。
- 消除特效当前运行资产为 `Assets/Prefab/Mahjong/MahjongEliminationFragments2.prefab`，其整体右上抛射轨迹被确认接近目标；材质为 `Assets/Material/MahjongEliminationFragments.mat`（主贴图 `Assets/Texture/Mahjong/EliminationParticleCircle.asset`）。该预制体采用单层圆形粒子：0秒一次发射76–84个，发射半径0.055、起始速度0、初始尺寸0.22–0.42；Velocity over Lifetime 在0–0.12秒保持 X/Y 为0形成成团蓄势，随后同步提升至右上方向（X最高2.5、Y最高5.8），叠加2倍重力下落，生命周期0.9–1.25秒并缩小淡出。`Assets/Resources/UI/Panel/GameScenePanel.prefab` 的 `GameScenePanel/Root/MahjongGameplay/EliminationEffectRoot` 持有两个失活预创建的 `MahjongEliminationFragments2` 实例；`MahjongGameplayView` 在 Awake 只收集该节点的直接子级根粒子并维护专用粒子对象池，池不足才基于首个实例创建。消除缩放结束的回调在 `MahjongCell.AnimateEliminated` 中触发，由 `MahjongGameplayView` 以卡牌世界坐标播放粒子；`StopGameplay` 会终止并回收所有该类特效。旧版 `Assets/Prefab/Mahjong/MahjongEliminationFragments.prefab` 不再被运行时池使用。
- `Assets/Texture/Mahjong/cell/` 当前为42张标准麻将牌：typeId 1–9 对应 `1tong`–`9tong`，10–18 对应 `1tiao`–`9tiao`，19–27 对应 `1wan`–`9wan`，28–34 为东南西北、中发白，35–42 为春夏秋冬梅兰竹菊（文件名以 `hua` 前缀区分冬与东风）。`Assets/Resources/Mahjong/MahjongCardVisualCatalog.asset` 已按该顺序完整映射。
- 卡牌贴图尺寸不统一时，运行时 `MahjongCell` 的 Icon 保留预制体固定区域与 Image 的 Preserve Aspect，不调用 `SetNativeSize()`。关卡编辑器 `MahjongLevelEditorWindow` 的 `DrawCard` 则按贴图原生尺寸绘制，并通过窗口 `Level Settings` 中持久化的 `Icon Scale` 滑条（范围0.01–1，默认0.19）统一缩放预览。
- `MahjongCell` 入槽时自身从 `Vector3.one` 缩放到 `MahjongViewConfig.SlotCardScale`，缩放与点击入槽/拖拽入槽的移动补间同时开始；`AnimateBack`、`AnimateReturnToBoard` 和 `ResetForPool` 会恢复为 `Vector3.one`。`GameScenePanel.prefab` 的 `SlotRoot` 保持原始局部缩放 `Vector3.one`。
- `MahjongGameplayView` 在每组卡槽消除动画结束并完成逻辑移除后，累计该组消除卡牌数；每局开局清零。累计达到 `MahjongConfig.RewardTriggerEliminatedCardCount`（10张）时，以 `RewardInitialProbability`（50%）首次判定；之后每多消除一组（2张）增加 `RewardProbabilityIncreasePerGroup`（10%），成功时无参打开 `GeneralRewardsPanel` 并清零累计数。
- `Assets/Prefab/Effect/FlickerEffect.prefab` 保留原有光束根 Animation，并新增 `StarFlicker` 子节点及独立 `Assets/Animation/FlickerEffectStarAnim.anim`。该节点包含 7 颗不同大小和位置的星星，以错峰透明度与缩放循环闪烁；最低透明度为 0.28，避免星星完全消失。
<!-- locus:body:end -->
