# Git_Mahjong 项目长期记忆

Unity 2022.3.62f3c1，Happy Mahjong: Rich Adventure；DOTween、AppLovin MAX、Adjust、SolarEngine、TMP 3.0.9；Built-in 管线（参考工程可能 URP）。用户自己重打 AB/出包，不代跑；不进 Play/不为验证加载场景。已在 Play 的问题只做用户要求的只读取证。

## 资产与迁移
- `Assets/Editor/ABbuildEditor.cs` 只收集 `Assets/ABRes/**` 打单包 `StreamingAssets/abres.dat`；meta 的 `assetBundleName` 非空会被排除；复制资产连 meta，排查悬空 guid（特别是动态 TMP 字体源文件）。AB manifest 判依赖进包看 `ClassTypes`，非 `Dependencies: []`。
- 搬 prefab 子树须复制 RectTransform 全字段并做 diff；跨工程嵌套 prefab stripped `m_CorrespondingSourceObject.fileID` 必须匹配导入后真实 fileID；CRLF 先正规化。用户排好的 prefab 不擅自改；真要改单属性优先 UnitySkills REST `prefab_set_property`，`gameObjectName` 用相对根路径，YAML `m_Name: 'X '` 的引号非名字、尾部空格是名字。
- 参考：`D:/UnityTest/majiangtext`（Tx）、`D:/Backup/Downloads/Packages/Assets`（PhaseTask）。UIParticle 在 `Assets/ParticleEffectForUGUI-3.3.13/`；彩带在 `Assets/Effect/Caidai/`（LeftCaidai 默认 scale=0）；过场钞票雨在 `Assets/guochang/`。

## Tx / PhaseTask
- Tx 脚本 `Assets/scripts/Tx/`，AB 资源 `Assets/ABRes/Tx/`，SO `Assets/Resources/Tx/`；弹窗音效由各 `Refresh()` 显式调用 `TxGameBridge.PlaySfx()`。
- `PhaseTaskRuntime.Update` 无条件 Tick；过档需活跃≥60秒且逃脱数达标；`NotifyArrowEscaped` 在 `MahjongGameplayView.CompleteEliminationAnimation()` 首行。注册表 HKCU `Software/Unity/UnityEditor/Happy Mahjong: Rich Adventure/Happy Mahjong: Rich Adventure` 的 `Tx.PhaseTask.SaveData_...` REG_BINARY 解码 JSON 可只读阶段状态。
- `TxHudSliders.prefab` 纯显示 HUD，79 个 Graphic 的 raycastTarget 都已关；修改它需用户明确授权并由用户自己重打 AB。

## 麻将数据与显示
- `Resources/Mahjong/Levels.json` 998关、`TutorialStages.json` 3关，皆固定牌型 `randomizeTypeIds:false`；`MahjongLevelRandomizer.CreateRandomizedLevel` 现在尊重 flag，false 时 CloneLevel；第三引导牌序由数据决定，不在代码写死。Resources 和脚本改动需重新出包，不是重打 AB。
- `MahjongCardVisualCatalog.asset` 已按 CardTypeMapping 重排：1–9 万、10–18 筒、19–27 条、28–31 东南西北、32–34 红中发财白板、35–38 梅兰竹菊、39–42 春夏秋冬。改 SO 用 REST export/import JSON，不手改 YAML；`jsonFilePath` 仅接受 `Assets/` 或 `Packages/`，可内联 json。
- 牌面链：JSON typeId → loader → randomizer（false CloneLevel）→ LayoutGenerator → 只读 `MahjongCardModel.TypeId` → visual catalog sprite。Shuffle 只交换坐标/层，不改 typeId。
- `InstanceId = cards 下标+1`；`coordX` 是行（上下），`coordY` 是列（左右），相邻牌相差2半格。显示大致 x=(coordY/2-(cols-1)/2)*163，y=(coordX/2-(rows-1)/2)*190；层偏移另计。取证：`gameobject_find(component=MahjongCell)` → `editor_playmode_inspect` 读 InstanceId/localPosition → `component_get_serialized_properties` 读 BlockedMaskObj 引用 → inspect 掩罩 activeSelf。
- 可点击：`MahjongBoardRules.ValidateSelection` 先 `IsCovered`，再 `HasBothSideNeighbors`；`MahjongGameplayView.RefreshBoardStates` 用相同规则开关 `BlockedMaskObj`。左右侧邻需在场、同 coordX、coordY±2，且 **邻牌 Layer≥目标 Layer**（允许上层跨层邻牌，低层牌不能挡住上层牌侧面）。2026-10-15 第99关第54张9筒 layer2：左南 layer2，右花兰 layer0；原不区分层时误挡，现排除右侧下层牌。被挡牌蒙黑遮罩但仍可点，点后抖动+音效；`NormalAlpha/BlockedAlpha` 均为1。
- Slot 系统是死代码，`MahjongSlotRules` 未实例化、`SlotFull` 不可达；真实配对 `MarkPairForElimination`。Idle Hint 在 `MahjongGameplayView.UpdateIdleHint`；Combo 计数由 `MahjongGameplayView.comboCount` 管，自动消除不算连击。

## 埋点（SolarEngine via OtherSdkManager）
- 上报唯一出口 = `OtherSdkManager.Instance.CustomEvent(...)` → `SolarEngine.Analytics.track`。**四个重载**：`(name)`（无参事件，传空字典）/ `(name, key, value)` / `(name, key, value, key2, value2)` / `(name, key, value, key2, value2, key3, value3)`。`CustomEvent(string eName)` 单参重载是 2026-10-15 按用户要求新加的。
- **用户明确要求：不要包一层，直接调 `OtherSdkManager.Instance.CustomEvent`**（2026-10-15）。原有的 `TxGameBridge.Track(name)` / `Track(name, key, value)` 包装已删除；`MahjongGameplayView` 里也不要加 `Instance != null` 守卫——工程既有惯例就是直接调。带参事件的键常量仍在 `TxGameBridge`（`SourceLevelId/SourceNode/SourceAmount/SourceDay/SourceLevel`；`SourceValue`/`SourceSpace` 现已无人引用）。
- **事件名直接用字符串字面量写在调用点，没有集中常量表**：`SolarEventNames.cs` 曾在 2026-10-15 按策划文档 1.1 的 8.4 表建过一版（`guide_click_start1~3`、`cash_page_enter`、`cashout_task_1~5`、`cashout_complete_1~4`、`bonus_page_enter`、`bonus_ad_click`），**当天即被用户要求整类删除**（"别多此一举"→"把这个去掉"）。要改事件名请全局搜字符串。
- 触发点：`cash_page_enter` = `CandyPanel.Open` 首次打开；`cashout_task_1~5`/`cashout_complete_1~4` = `PhaseTaskService` 的 `TrackTask1~5`/`TrackComplete`（带递增去重与 completeMask 去重）；`guide_click_start1` = `GuidePop.OnStartClicked`，`2/3` = `GuidePanel_firstGame` 手势引导首次点击（按 `FirstGameGuideData.stageIndex`）；`bonus_page_enter` = `MahjongGameplayView.ShowLuckyReward()`；`bonus_ad_click` = `GeneralRewardsPanel.AdsCallback()` 且 `data == null`（Lucky 弹窗传 null，任务领奖传 TaskData）。
- `cashout_task_*` 的值口径（2026-10-15 核实）：`task_1` 键 `level_id`，值 = 本次通关的主玩法关卡号 `completedLevel`；`task_3` 键 `amount`，值 = `CurrentGold()`；`task_5` 键 `level`，值 = `saveData.playerLevelProgress.level`（**PhaseTask 等级轴，不是 `playerInfo.level`**）。`TxGameBridge.SourceLevel`/`SourceLevelId` 等常量只是参数**键名**，不是取值来源。`TrackIncreasing` 只上报递增新高（`sequence > mark.taskN`）；`task_5` 还要求 `phaseAtEntry == PhaseId.PlayerLevel30` 且本次等级确实提升。
- 三个"等级"口径别混：① `playerLevelProgress.Level` = `PlayerLevelCalculator.Calculate(totalCompletedLevels, LevelRequirements).Level` → `SettingPanel.gradeText`（"Player Level:"）与 `cashout_task_5` 都用它；② `TxGameBridge.CurrentLevel` = `playerInfo.level`（主玩法关卡号）→ `GetLevelDisplayLevel/GetLevelDisplayNumber` 用；③ `GetLevelDisplayNumber()` = ② 过 `PhaseOneHudCalculator` → `GameScenePanel` 顶部 "Level XX" / HUD。①②是两个量，不一致属正常。

## 编译与工具
- UnitySkills 需编辑器面板启用才监听 8090；按用户约定只在需要时 asset_refresh + debug_get_errors + dll 时间戳/Editor.log 最近编译状态，不做运行功能验证。不要在编辑器已开时用 batchmode。`Editor.log` 旧 `error CS` 会累积，不能简单全文计数。
- Play 模式 `editor_playmode_inspect` 若运行态变化，应重新 `gameobject_find` 取当下 instanceId；同名多张牌要用 ID 明确指向，`component_get_serialized_properties` 的引用值形如 `-123:UnityEngine.GameObject:BlockedMask`（**第一个字段才是 instanceId**）。
- Windows Git Bash 的 heredoc `:\n` 可能变 `:/n`，正则脚本用 Write 工具写文件再跑。不要重复验证同一件事；只改用户要求的范围，不主动加新注释。