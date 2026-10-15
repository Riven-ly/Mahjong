# Git_Mahjong 项目长期记忆

Unity 2022.3.62f3c1，产品 "Happy Mahjong: Rich Adventure"。技术栈：Unity + DOTween + AppLovin MAX + Adjust + SolarEngine + TextMeshPro 3.0.9。

## AB 包机制

- 打包脚本 `Assets/Editor/ABbuildEditor.cs`，MenuItem `Tools/AB打包/打包AB包`。
- 只收集 `Assets/ABRes/**`，打成一个 `abres.dat`（AES-CBC），输出 `Assets/StreamingAssets/abres.dat`。
- **重打 AB 包由用户自己执行**，不代跑。
- **坑**：连 meta 复制的资产可能带 `assetBundleName: xxx.dat` → 被排除出 `abres.dat`。修复 = 清空 `assetBundleName`。排查：`grep -rn "assetBundleName: .\+" Assets --include="*.meta"` 应为 0。
- **坑**：连 meta 复制的资产内部 guid 引用可能悬空（尤其 `TMP_FontAsset.m_SourceFontFile`）。Dynamic 字体拿不到源 otf → 文字不渲染。
- **AB manifest 读法**：`Assets:` 段只列显式资源；判入包看 `ClassTypes:` 段（按类型去重）。`Dependencies: []` 不代表依赖没进包。
- TMP shader 加入 `ProjectSettings/GraphicsSettings.asset` 的 `m_AlwaysIncludedShaders`（已加 7 个 SDF 系列）。

## 搬运 prefab 子树铁律

- 必须连 RectTransform 的锚点/位置/尺寸/轴心一起复制，搬完做 RectTransform 级 diff。
- 嵌套 prefab stripped 块的 `m_CorrespondingSourceObject.fileID` 必须等于目标工程导入后的真实 fileID。
- 参考工程 `.prefab` 是 CRLF，Python 匹配前先 `text.replace("\r\n", "\n")`。
- 跨工程移植弹窗脚本：参考 `ViewBase`/`PopupBase` 有 DOTween 弹窗框架，本项目 `UIBase` 只有 `panelAnim`。绝不在子类定义 `OnEnable/OnDisable`（基类 private）。
- 手写新建资源 .meta：自己生成 32 位 hex guid，`asset_refresh` 后回读确认。
- 改 prefab 磁盘 YAML 是否被回滚取决于编辑器是否已加载该 prefab。

## 参考工程

- `D:/UnityTest/majiangtext` ← Tx 提现弹窗来源。`LevelPopup.prefab` 两工程 fileID 相同。
- `D:/Backup/Downloads/Packages/Assets` ← PhaseTask 五阶段任务来源（EscapePuzzle 3D 箭头玩法）。排查 Task1~5 以此工程为准。
- **渲染管线不同**：参考工程是 **URP 14.0.12**（`Assets/PipleSetting/New Universal Render Pipeline Asset.asset`）；本项目是 **Built-in**（`GraphicsSettings.m_CustomRenderPipeline: {fileID: 0}`，manifest 无 URP 包）。迁移特效时注意：用内置 shader（如 `Particles/Standard Unlit`）的材质在本项目正常，在参考工程反而可能异常。

## 第三方插件（已迁入）

- `Assets/ParticleEffectForUGUI-3.3.13/` —— Coffee.UIParticle（mob-sakai，MIT），让 ParticleSystem 直接在 uGUI 里渲染（免 Camera/RenderTexture）。asmdef：`Coffee.UIParticle`（运行时）+ `Coffee.UIParticle.Editor`。**无外部 guid 依赖**，可整目录连 `.meta` 复制。`UIParticle.cs` guid = `16f0b0b6d0b7542bfbd20a3e05b04ff1`。
- 迁移外部插件/特效的通用做法：**整个目录连 `.meta` 一起 cp**（guid 原样保留，引用不断），然后 `asset_refresh`。

## 特效资产

- `Assets/Effect/Caidai/` —— 彩带飘落粒子（`LeftCaidai` / `RightCaidai` / `DownCaidai` + `confetti.png` + `confetti_AB.mat`），来自参考工程 `ArtResources/Effect/Caidai/`，连 meta 复制 guid 不变（LeftCaidai = `5bd8d0c11e799bc4e8ad2d9655a55a40`）。
  - 依赖 UIParticle；`LeftCaidai` 根 RT **自带 `m_LocalScale: {0,0,0}`（默认隐藏）**，用时必须显式放大。
  - ParticleSystem：5 秒 / `playOnAwake: 1` / 不循环 → SetActive 打开即播。
- `Assets/guochang/` + 场景 `Game.unity` 的 `GuoChang/guochang/CashRainParticles` —— 过场钞票雨粒子（纯 ParticleSystem 包，无脚本）。`GameManager.guoChangRoot` 引用 `guochang`，`PlayGuoChang(bool)` = `SetActive`。

## Tx（提现）模块

- 脚本 `Assets/scripts/Tx/` + 桥接 `TxGameBridge.cs`；AB 资源 `Assets/ABRes/Tx/`；字体在 `Assets/Font/`（AB 包外）。
- 配置 SO：`Assets/Resources/Tx/PhaseTaskConfig.asset`、`PhaseOneHudConfig.asset`。
- 挂载点：`ABResManager.prefab` 拖引用 `txWithdrawBarPrefab` / `txTipsRootPrefab`，`Init()` 实例化到 `PlayerInfoUI`。
- 埋点 `OtherSdkManager.Instance.CustomEvent(...)`；金币 `GameManager.Instance.playerInfo.Gold`；本地化 `LanguageManager.GetText(key)`。
- **弹窗音效**：各 Tx 弹窗在 `Refresh()` 里**显式**调 `TxGameBridge.PlaySfx()`（= `AudioManager.PlayBtnMusic()` → `btnMusic` 音频源播 `Assets/Audio/btn.mp3`）。全工程 18 处（CandyPanel / LevelPopup / InProgressPop / SpecialBonusPop / RecordPop / PhaseTaskPanel / ProcessCompletePop / TxWithdrawBar / AcCandyPop / ConfirmAcPop / AcSelectContainer / GuidePop）。`UIBase` / `UIManager` 本身不播音，所以面板有音效一定是自己调的。

## PhaseTask 关键事实

- `Tick` 由 `PhaseTaskRuntime.Update` 无条件驱动（原始设计，别"修"）。
- 过一档 = `活跃满60秒` 且 `successfulEscapes >= 档位目标`（与关系）。
- `NotifyArrowEscaped()` 挂载点 = `MahjongGameplayView.CompleteEliminationAnimation()` 首行（每消除一对牌 +1）；另在 `DebugCompleteLevel()` 里按剩余牌对数循环补发（仅调试路径）。2026-10-14 已补，10-15 复核两处均在。
- **运行时读阶段存档（免进 Play）**：`Tx.PhaseTask.SaveData` 落在注册表 `HKCU\Software\Unity\UnityEditor\Happy Mahjong: Rich Adventure\Happy Mahjong: Rich Adventure`，值名带 hash 后缀（如 `Tx.PhaseTask.SaveData_h4174998170`），REG_BINARY 字节数组直接解码即 JSON。`Tick` 每跨 1 秒整数就 `Save()`，所以 `activeSeconds` 可实时观测。注意 `reg.exe` 被安全策略禁用，用 PowerShell `[Microsoft.Win32.Registry]::CurrentUser` 读、结果写文件再读。
- Task3 金币用 `TxGameBridge.GoldAmount`；Task5 等级用 `TxGameBridge.CurrentLevel`（=`playerInfo.level`）。
- `PhaseTaskSaveData.cAmount` 是死字段，保留不用。

## 第一关3小关卡引导系统

- 配置：`Assets/Resources/Mahjong/TutorialStages.json`（3关：2张/6张/4张），从 `D:/Backup/Downloads/Levels-998/` 原样导入。
- 加载：`TutorialStageCatalogLoader.CreateRuntimeStage(index)`，第3小关随机出牌型后 `RearrangeStage3()` 重排为 A,B,B,A。
- 触发链路：第1小关 → LevelTaskTip 关闭 callback → `TryOpenFirstGameGuide()`；第2、3小关 → `StartFirstGameStage()` 里直接调。
- per-stage 标记：`FirstGameGuideCompleted` / `...2` / `...3`（PlayerPrefs）。
- `GuidePanel_firstGame` 的 `textKey` 字段控制每关引导文案。

## 闲置引导（Idle Hint）

- 在 `MahjongGameplayView.Update()` → `UpdateIdleHint()` 实现。
- 默认等待 10 秒，高亮 2 秒，不消耗道具/不播音效。支持远程配置（`idle_hint_wait` / `idle_hint_duration`）。
- 首关教学期间不触发（`IsFirstGameTutorialActive()`）。
- 玩家点击/道具操作时 `ResetIdleHintTimer()` 重置计时。
- `MahjongConfig.IdleHintWaitSeconds` / `IdleHintActiveSeconds` 为默认常量。
- 远程配置通过 `MahjongGameplayView.RemoteIdleHintWaitSeconds` / `RemoteIdleHintActiveSeconds` static 属性设置。

## 连击（Combo）特效

- 资产包 `Assets/CashRewardsComboEffects/`（自包含 UGUI + Spine 4.2 特效，含 `README-中文.txt`）。
- 核心 `CashRewards.ComboEffects.ComboEffectPlayer.PlayCombo(int count)`，**不维护计数**；阈值写死：3 起 Combo / 5,10 Perfect / 15,20 及 30 起整十 Excellent。
- 计数在 `MahjongGameplayView.comboCount`。挂点：手动配对成功 +1、配对失败清零、手动洗牌清零、`StartNewGame` 清零；自动洗牌（`EnsurePlayablePair`）与取消选择**不清零**。
- 自动消除（返回道具）**不算连击**（不走 `HandleCellSelectRequested`）。
- `comboEffects` 已在 `GameScenePanel.prefab` 接好：ComboEffects 作为嵌套 prefab 实例挂在 `MahjongGameplay` 节点下（新增 fileID `9000000000000000001`=PrefabInstance / `...002`=stripped RT / `...003`=stripped ComboEffectPlayer）。

## 麻将主玩法（MahjongGameplayView）

- 视图 `Assets/scripts/Mahjong/View/MahjongGameplayView.cs`，单张牌视图 `MahjongCell.cs`（含 `canvasGroup` 控透明度、`rectTransform` 控缩放/位置）。
- 关卡创建链：`GameScenePanel.Refresh()` → `gameplayView.StartNewGame(levelDef)` → `BuildBoardViews()` → `EnsurePlayablePair()`。
- **入场动效**（2026-10-15 加）：`StartNewGame` 末尾调 `PlayEntranceAnimation()` —— 每张牌 `canvasGroup` alpha 0→1 淡入，延迟按**从牌面左上角起的对角（Manhattan）距离**扩散。参数常量在 `MahjongViewConfig.EntranceDuration / EntranceStagger`，格距常量在 `MahjongGameplayView.EntranceCellPitchX/Y`（= `BoardCellWidth/Height * 0.5f`，即相邻牌实际间距）。
  - 用户明确要的是**扩散**（从左上角第一张向外），不是行优先逐行扫。改顺序时别退回 `i * stagger`。
  - **铁律**：入场必须排在 `EnsurePlayablePair()` **之后**。它内部 `RefreshBoardStates()` → `MahjongCell.SetBlocked()` 会把 `canvasGroup.alpha` 写回 1，放前面会被覆盖。
  - 入场期间 `SetBoardInput(false)`，避免点到半透明的牌。
- 表现配置统一放 `MahjongViewConfig`（静态常量类），不要散在方法里。
- **调试入口三层范式**：`Assets/scripts/Manager/Editor/GameManagerEditor.cs` 的 `OnInspectorGUI` 加按钮 → `GameManager.DebugXxx()`（带 `[ContextMenu("Debug/…")]`）→ 面板/视图的 `DebugXxx()`。新增调试功能照此走，别自创机制（工程虽有 SRDebugger 包，但业务调试入口都走这条链）。

## 用户手工排布的 prefab（不要动结构）

- `TxWithdrawBar.prefab` / `TxHudSliders.prefab` — 用户改过，只改脚本不碰 prefab。

## guid 映射（参考 → 本项目）

| 资产 | 参考 guid | 本项目 guid |
|---|---|---|
| neuron-extrabold SDF | 9e8d8050d99c2cb49b754817ec264f9a | d6389bd164024a56ba3421ce9947bde4 |
| Atlas White Outline Material | 193391c627b444e1ac679b45c076f013 | fb89e49c0b6545c4a9f884e2dee61fd5 |
| badge.png | 3447c0dcde9e2cd45b9273286a93d0bd | e557dd68370d40ec9c6c1e3ce75654c6 |

美术类资产迁移后 guid 全变；预制体连 meta 复制 guid 不变。搬 YAML 前必须建全量 guid 重映射表。
