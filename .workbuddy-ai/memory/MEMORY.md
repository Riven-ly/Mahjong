# Git_Mahjong 项目长期记忆

Unity 2022.3.62f3c1，产品 "Happy Mahjong: Rich Adventure"。技术栈：Unity + DOTween + AppLovin MAX + Adjust + SolarEngine + TextMeshPro 3.0.9。

## AB 包机制（关键）

- 打包脚本 `Assets/Editor/ABbuildEditor.cs`，MenuItem `Tools/AB打包/打包AB包`。
- `AssetDatabase.FindAssets("t:Object", new[]{"Assets/ABRes"})` → **只收集 `Assets/ABRes/**` 下的资源**，打成一个 `abres.dat`（AES-CBC），输出 `Assets/StreamingAssets/abres.dat`。
- 加载：`GameManager.LoadAssetBundleAsync()` → `ABbuildManager.LoadABAsync()` → `LoadAssetAsync<GameObject>("assets/abres/abresmanager/abresmanager.prefab")` → `Instantiate`。
- **AB 包外的资源（如 `Assets/Font/`、`Assets/TextMesh Pro/`）只能作为「隐式依赖」被带进包**，前提是其 `.meta` 的 `assetBundleName` 为空。
- **重打 AB 包由用户自己执行**，不代跑。改完只需说明「需要重打 AB 包」。

### 坑：`.meta` 的 assetBundleName 会跟着「连 meta 复制」一起搬进来

从源工程「连 meta 复制」的资产，如果源工程给它标了 `assetBundleName: xxx.dat`，这个标记会一起进来。
后果：打 `abres.dat` 时 Unity 把该资产判定为「属于另一个包」，**不写进 `abres.dat`**（manifest 的 `Dependencies: []`），
运行时只加载 `abres.dat` → 该资产（及其材质/shader）解析失败。

**实例（2026-10-13）**：`Assets/Font/` 下 4 个 `.meta` 带 `assetBundleName: demores.dat`（含 `neuron-extrabold SDF.asset.meta`），
导致 TMP 文字全部不渲染（字体材质丢失）。修复 = 清空 `assetBundleName`。

**排查手法**：`grep -rn "assetBundleName: .\+" Assets --include="*.meta"` 应恒为 0 命中；
解析 `abres.dat.manifest` 的 `Assets:` 段 + `Dependencies:` 段确认依赖是否入包。

### 坑：连 meta 复制的资产，内部 guid 引用可能悬空

连 meta 复制的资产，其**文件内部**引用的其它资产 guid 是从源工程带过来的，目标工程里可能不存在。
必须逐个核查（尤其 `TMP_FontAsset` 的 `m_SourceFontFile` / `m_SourceFontFileGUID`）。

**实例（2026-10-13）**：`neuron-extrabold SDF.asset` 的 `m_SourceFontFile` 指向源工程 otf guid `61ec9349…`（目标不存在），
`m_AtlasPopulationMode: 1`（Dynamic 动态字体）会因此无法在运行时补字形 → 文字不渲染。
修复 = 重指向目标工程实际的同名 otf `Assets/Font/neuron-extrabold.otf`（guid `8e8f3049…`，同一 Neuron ExtraBold 字体）。

**TMP 文字不渲染的排查顺序**：
1. `m_fontAsset` / `m_sharedMaterial` 的 guid+fileID 能否解析（`type: 2` = 字体资产内嵌子对象）
2. 字体资产是否自包含（`!u!114` 本体 + `!u!21` 材质 + `!u!28` atlas 贴图，三者引用闭合）
3. 字体资产的 `.meta` 有无 `assetBundleName`（会被排除出 `abres.dat`）
4. 字体资产的 `m_SourceFontFile` 是否悬空（Dynamic 字体必须能拿到源 otf/ttf）
5. 材质 Inspector 的 `Face → Texture = None` **不是问题**（TMP 只用 `_MainTex`，`_FaceTex` 本就空）

### 坑：`Assets/StreamingAssets/` 被用户清空 = 正在重打包

打包脚本会先 `File.Delete` 旧 `abres.dat`。若该目录为空，说明用户正在重打或打包失败，**不要介入**。

### AB manifest 的正确读法（重要，曾误判）

`abres.dat.manifest` 里：
- **`Assets:` 段只列「显式指定」的资源（即打包脚本 `assetNames` 传入的那些），依赖不列出。**
  所以「`Assets:` 段没有 Foo」**不等于**「Foo 没进包」。
- **判据看 `ClassTypes:` 段** —— 它列出**包里实际存在**的对象类型（按类型去重）。
  例如含 `Class: 114` + `TMP_FontAsset.cs`(guid `71c1514a…`) 就证明字体资产在包里；
  `Class: 48`=Shader、`Class: 21`=Material、`Class: 28`=Texture2D、`Class: 74`=AnimationClip、
  `Class: 95`=Animator、`Class: 1`=GameObject、`Class: 4`=Transform、`Class: 91`=AnimatorController。
- `Dependencies: []` 为空**不代表依赖没进包**；依赖已在同包内时不列。

### TMP + AssetBundle 的坑

- TMP 字体材质的 shader（`TMP_SDF.shader`，guid `68e6db2e…`）如未被显式包含，AB 加载后 shader 引用可能解析为 null，
  表现 = 材质/文字丢失，**手动在 Inspector 重赋一次 shader 才恢复**。
- 缓解：把 TMP SDF shader 系列加入 `ProjectSettings/GraphicsSettings.asset` 的 `m_AlwaysIncludedShaders`
  （格式 `- {fileID: 4800000, guid: <guid>, type: 3}`）。已加 7 个（SDF / Mobile / SSD / Mobile SSD / Overlay / Mobile Overlay）。
- 字体资产（`TMP_FontAsset`）的 `.meta` **不能带 `assetBundleName`**，否则依赖不进 `abres.dat`。

## 搬运 prefab 子树的铁律

- **「一比一复制」必须连 RectTransform 的 `m_AnchorMin` / `m_AnchorMax` / `m_AnchoredPosition` / `m_SizeDelta` / `m_Pivot` 一起复制**，
  不能只搬节点名与组件类型。搬完必须做 **RectTransform 级 diff**。
- **嵌套 prefab 的 stripped 块**：`m_CorrespondingSourceObject.fileID` 必须精确等于**目标工程导入后**该对象的真实 fileID
  （不是源工程的 fileID）。用错 → Unity 回退成 `long.MaxValue` 占位 → `Duplicate identifier 9223372036854775807`。
- 写回文件用 LF（`newline=chr(10)`）。
  **注意：参考工程 `D:/UnityTest/majiangtext/Assets/**` 的 .prefab 是 CRLF**，
  用 Python 按行匹配字段块前必须先 `text.replace("\r\n", "\n")` 归一化，否则匹配恒失败。
- **跨工程移植 UI 弹窗脚本的体系适配**：参考工程 `ViewBase`(→`PopupBase`) 有整套 DOTween 弹窗框架
  （序列化字段 `animationDuration/closeAnimationDuration/animationType/animationTarget/slidePadding/background`），
  本项目 `UIBase` 只有 `panelAnim`(Animation)。移植时：
  ① prefab 脚本块把这 6 个字段换成本项目 `uIPanelLayer/isOpen/panelAnim`；
  ② 面板滑入/背景淡入改到 `Refresh()` 里自播 DOTween Sequence；
  ③ 关闭用 `UIManager.Instance.HideUI<T>()`（`UIBase.Hide()` 在 `panelAnim==null` 时立即失活 + 触发 `callback`）；
  ④ **绝不在子类定义 `OnEnable/OnDisable`** —— `UIBase` 里这俩是 private，子类一写就"隐藏"基类的，`isOpen` 会坏；清理放 `Hide()` override。
- **手写新建资源（.cs / .prefab / .png）的 .meta 时**：自己生成 32 位 hex guid 写进 meta，Unity 会沿用；
  但 `asset_refresh` 后必须**回读 meta 确认 guid 未被改写**，并 grep `Editor.log` 确认按该 guid 导入、无 missing script。
- **直接改 prefab 磁盘 YAML 是否会被 Unity 回滚，取决于该 prefab 当时是否被编辑器加载**：
  未被加载的（只存在于 `Resources/`、`ABRes/`，编辑器没打开）→ 改盘 + `asset_refresh` **不会被回滚**（2026-10-13 实测 `SettingPanel.prefab`）；
  已被加载/打开的（Prefab Stage、或在打开的场景里）→ 会被内存版本覆盖。
  改前先备份、改后 `asset_refresh` + 读回文件 + `debug_get_errors` 三重确认。

## Tx（提现）模块结构（目标工程）

- 脚本：`Assets/scripts/Tx/`（`Domain/` `Runtime/` `Config/` `UI/` `Sdk/`）+ 桥接 `TxGameBridge.cs`。
- AB 资源：`Assets/ABRes/Tx/`（`UI/Panel/` 预制体、`Demo/` 美术）；字体在 **`Assets/Font/`**（AB 包外）。
- 配置 SO：`Assets/Resources/Tx/PhaseTaskConfig.asset`、`PhaseOneHudConfig.asset`。
- 挂载点：`ABResManager`（`Assets/ABRes/ABResManager/ABResManager.prefab`）拖引用 `txWithdrawBarPrefab` / `txTipsRootPrefab`，
  `Init()` 里实例化到 `PlayerInfoUI` 的 `txTrans` / `txTipsRootTrans`。
- 关键约定：`ABResManager.prefab` 从 AB 实例化，但序列化字段**可直接引用** `Assets/ABRes/Tx/UI/Panel/*.prefab`
  （格式 `{fileID: <prefab根GO fileID>, guid: <prefab guid>, type: 3}`）。
- 埋点：`OtherSdkManager.Instance.CustomEvent(...)`；金币口径：`GameManager.Instance.playerInfo.Gold`。
- 本地化：`LanguageManager.GetText(key)` / `GetText_Encrypt(key)`（值 Base64）。

## 用户手工排布过的 prefab（不要动结构）

- `Assets/ABRes/Tx/UI/Panel/TxWithdrawBar.prefab`（用户 16:30 改过，包了一层 `GameObject` + 改名 `tttt`）
- `Assets/ABRes/Tx/UI/Panel/TxHudSliders.prefab`（用户 17:13 重构过，272KB→120KB）
- 用户说「我自己搞定」时，**只改脚本，不碰 prefab**。

## PhaseTask（五阶段任务）模块 —— 两个参考工程，别搞混

- **`D:/UnityTest/majiangtext`** ← Tx 提现弹窗（Sdk/Prefabs）的来源。
- **`D:/Backup/Downloads/Packages/Assets`** ← **PhaseTask 五阶段任务模块的来源**
  （EscapePuzzle 3D 箭头玩法；模块在 `Demo/Script/GamePhaseTask/**`）。
  排查 Task1~5 / TxHudSliders 的一切行为，**以此工程为准**。

### 关键事实（2026-10-14 实测）

- **`Tick` 由 `PhaseTaskRuntime.Update` 无条件驱动；`SetGameplayActive` 是空实现** ——
  参考工程同样如此（`:361`），**是原始设计，不是缺陷，别去"修"它**。
  → 阶段为 `ActiveMinutes20` 时，游戏开着就在累计时长（后台也计时）。
- **过一档 = `活跃满60秒` 且 `successfulEscapes >= 档位目标`**（`TryCompleteMinuteSlot`），是「与」关系。
- **`NotifyArrowEscaped()` 的真实调用点：参考工程 `Scripts/Puzzle/PuzzleController.cs:292`**
  （`IEnumerator Escape()` 协程尾，每逃出 1 个箭头 +1）。
  **本项目对应挂载点 = `MahjongGameplayView.CompleteEliminationAnimation()` 首行**
  （`Assets/scripts/Mahjong/View/MahjongGameplayView.cs`，每对牌消除的统一出口），
  粒度 = **每消除一对牌计 1 次**。**2026-10-14 已补上**（此前完全缺失 → Task2 恒卡 1/20）。
- Task3 金币源：参考用 `CoinAmount.Count`（`Scripts/Puzzle/CoinAmount.cs`），
  本项目用 `TxGameBridge.GoldAmount` —— **行为等价，不要搬 CoinAmount**。
- Task5 / 阶段1 等级：参考用 `StageLoader.CurrentStage`（`Scripts/Puzzle/StageLoader.cs:8`，= 当前关卡号），
  本项目用 `TxGameBridge.CurrentLevel`（=`playerInfo.level`）—— **等价**。
  `playerLevelProgress.level`（阶段任务自己的等级轴，由 `totalCompletedLevels` 推）与
  `playerInfo.level`（主玩法关卡号）**本就是两个量，不一致是正常的，不是 bug**。

### 遗留（未动）
- `PhaseTaskSaveData.cAmount` 是**死字段**（无任何读写点），保留不用。
- 判据备忘：`Editor.log` 中段的 `ABResManager.cs` CS0246（`TxWithdrawBar`/`TipMgr`）是**陈旧记录**，
  判断是否有新错误要看「REST `debug_get_errors` 返回」+「日志新增段的 `error CS` 计数」。

## 参考工程 `D:/UnityTest/majiangtext`（Tx 模块的源工程）

- 结构对应：参考 `Demo/Script/Sdk/**` + `Demo/Prefabs/SDK/**` ↔ 本项目 `Assets/scripts/Tx/**` + `Assets/ABRes/Tx/**`。
- **两个工程的 `LevelPopup.prefab` 结构完全一致（连 fileID 相同，根 GO `3000`）** —— 可用「逐字段对齐 guid」反推映射表。
- 参考工程设置弹窗 `Demo/Script/EscapePuzzle/B_SettingPopup.cs` 的 `gradeButton`（节点 `LevelButtonText (TMP)`，
  同 GO 挂 TMP + Button）→ `OpenLevelDetail()` → `Show<LevelPopup>()` + 隐藏设置；另有 `IDText (TMP)`、`Ico`(badge.png)。
  本项目 `SettingPanel.cs` 已按此一比一补上 `idText` / `gradeText` / `gradeButton`（挂在 `panel2/bg` 下）。

### guid 映射（参考 → 本项目）
| 资产 | 参考 guid | 本项目 guid |
|---|---|---|
| `neuron-extrabold SDF.asset` | `9e8d8050d99c2cb49b754817ec264f9a` | `d6389bd164024a56ba3421ce9947bde4` |
| `neuron-extrabold Atlas White Outline Material.mat` | `193391c627b444e1ac679b45c076f013` | `fb89e49c0b6545c4a9f884e2dee61fd5` |
| `badge.png`（`Demo/Sprite/Ico/`） | `3447c0dcde9e2cd45b9273286a93d0bd` | `e557dd68370d40ec9c6c1e3ce75654c6` |

美术类资产在迁移时被重新导入 → guid 全变（本项目在 `Assets/ABRes/Tx/Demo/`）；
预制体是「连 meta 复制」→ 文件 guid 不变。**搬 YAML 前必须按 basename 建全量 guid 重映射表。**
