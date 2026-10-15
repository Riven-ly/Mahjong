# Git_Mahjong 项目长期记忆

Unity 2022.3.62f3c1，Happy Mahjong: Rich Adventure；DOTween、AppLovin MAX、Adjust、SolarEngine、TMP 3.0.9；Built-in 管线（参考工程可能 URP）。用户自己重打 AB/出包，不代跑；不进 Play/不为验证加载场景。已在 Play 的问题只做用户要求的只读取证。

## 资产与迁移
- `Assets/Editor/ABbuildEditor.cs` 只收集 `Assets/ABRes/**` 打单包 `StreamingAssets/abres.dat`；meta 的 `assetBundleName` 非空会被排除；复制资产连 meta，排查悬空 guid（特别是动态 TMP 字体源文件）。AB manifest 判依赖进包看 `ClassTypes`，非 `Dependencies: []`。
- 搬 prefab 子树须复制 RectTransform 全字段并做 diff；跨工程嵌套 prefab stripped `m_CorrespondingSourceObject.fileID` 必须匹配导入后真实 fileID；CRLF 先正规化。用户排好的 prefab 不擅自改；真要改单属性优先 UnitySkills REST `prefab_set_property`，`gameObjectName` 用相对根路径，YAML `m_Name: 'X '` 的引号非名字、尾部空格是名字。
- 参考：`D:/UnityTest/majiangtext`（Tx）、`D:/Backup/Downloads/Packages/Assets`（PhaseTask）。UIParticle 在 `Assets/ParticleEffectForUGUI-3.3.13/`；彩带在 `Assets/Effect/Caidai/`（LeftCaidai 默认 scale=0）；过场钞票雨在 `Assets/guochang/`。

## Tx / PhaseTask
- Tx 脚本 `Assets/scripts/Tx/`，AB 资源 `Assets/ABRes/Tx/`，SO `Assets/Resources/Tx/`；弹窗音效由各 `Refresh()` 显式调用 `TxGameBridge.PlaySfx()`。
- `PhaseTaskRuntime.Update` 无条件 Tick；过档需活跃≥60秒且逃脱数达标；`NotifyArrowEscaped` 在 `MahjongGameplayView.CompleteEliminationAnimation()` 首行。Task3 金币来自 `GoldAmount`，Task5 等级来自 `CurrentLevel`。注册表 HKCU `Software/Unity/UnityEditor/Happy Mahjong: Rich Adventure/Happy Mahjong: Rich Adventure` 的 `Tx.PhaseTask.SaveData_...` REG_BINARY 解码 JSON 可只读阶段状态。
- `TxHudSliders.prefab` 纯显示 HUD，79 个 Graphic 的 raycastTarget 都已关；修改它需用户明确授权并由用户自己重打 AB。

## 麻将数据与显示
- `Resources/Mahjong/Levels.json` 998关、`TutorialStages.json` 3关，皆固定牌型 `randomizeTypeIds:false`；`MahjongLevelRandomizer.CreateRandomizedLevel` 现在尊重 flag，false 时 CloneLevel；第三引导牌序由数据决定，不在代码写死。Resources 和脚本改动需重新出包，不是重打 AB。
- `MahjongCardVisualCatalog.asset` 已按 CardTypeMapping 重排：1–9 万、10–18 筒、19–27 条、28–31 东南西北、32–34 红中发财白板、35–38 梅兰竹菊、39–42 春夏秋冬。改 SO 用 REST export/import JSON，不手改 YAML；`jsonFilePath` 仅接受 `Assets/` 或 `Packages/`，可内联 json。
- 牌面链：JSON typeId → loader → randomizer（false CloneLevel）→ LayoutGenerator → 只读 `MahjongCardModel.TypeId` → visual catalog sprite。Shuffle 只交换坐标/层，不改 typeId。
- `InstanceId = cards 下标+1`；`coordX` 是行（上下），`coordY` 是列（左右），相邻牌相差2半格。显示大致 x=(coordY/2-(cols-1)/2)*163，y=(coordX/2-(rows-1)/2)*190；层偏移另计。取证：`gameobject_find(component=MahjongCell)` → `editor_playmode_inspect` 读 InstanceId/localPosition → `component_get_serialized_properties` 读 BlockedMaskObj 引用 → inspect 掩罩 activeSelf。
- 可点击：`MahjongBoardRules.ValidateSelection` 先 `IsCovered`，再 `HasBothSideNeighbors`；`MahjongGameplayView.RefreshBoardStates` 用相同规则开关 `BlockedMaskObj`。左右侧邻需在场、同 coordX、coordY±2，且 **邻牌 Layer≥目标 Layer**（允许上层跨层邻牌，低层牌不能挡住上层牌侧面）。2026-10-15 第99关第54张9筒 layer2：左南 layer2，右花兰 layer0；原不区分层时误挡，现排除右侧下层牌。被挡牌蒙黑遮罩但仍可点，点后抖动+音效；`NormalAlpha/BlockedAlpha` 均为1。
- Slot 系统是死代码，`MahjongSlotRules` 未实例化、`SlotFull` 不可达；真实配对 `MarkPairForElimination`。Idle Hint 在 `MahjongGameplayView.UpdateIdleHint`；Combo 计数由 `MahjongGameplayView.comboCount` 管，自动消除不算连击。

## 编译与工具
- UnitySkills 需编辑器面板启用才监听 8090；按用户约定只在需要时 asset_refresh + debug_get_errors + dll 时间戳/Editor.log 最近编译状态，不做运行功能验证。不要在编辑器已开时用 batchmode。`Editor.log` 旧 `error CS` 会累积，不能简单全文计数。
- Play 模式 `editor_playmode_inspect` 若运行态变化，应重新 `gameobject_find` 取当下 instanceId；同名多张牌要用 ID 明确指向，`component_get_serialized_properties` 的引用值形如 `-123:UnityEngine.GameObject:BlockedMask`（**第一个字段才是 instanceId**）。
- Windows Git Bash 的 heredoc `:\n` 可能变 `:/n`，正则脚本用 Write 工具写文件再跑。不要重复验证同一件事；只改用户要求的范围，不主动加新注释。