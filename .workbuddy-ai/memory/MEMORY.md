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

## Tx（提现）模块

- 脚本 `Assets/scripts/Tx/` + 桥接 `TxGameBridge.cs`；AB 资源 `Assets/ABRes/Tx/`；字体在 `Assets/Font/`（AB 包外）。
- 配置 SO：`Assets/Resources/Tx/PhaseTaskConfig.asset`、`PhaseOneHudConfig.asset`。
- 挂载点：`ABResManager.prefab` 拖引用 `txWithdrawBarPrefab` / `txTipsRootPrefab`，`Init()` 实例化到 `PlayerInfoUI`。
- 埋点 `OtherSdkManager.Instance.CustomEvent(...)`；金币 `GameManager.Instance.playerInfo.Gold`；本地化 `LanguageManager.GetText(key)`。

## PhaseTask 关键事实

- `Tick` 由 `PhaseTaskRuntime.Update` 无条件驱动（原始设计，别"修"）。
- 过一档 = `活跃满60秒` 且 `successfulEscapes >= 档位目标`（与关系）。
- `NotifyArrowEscaped()` 挂载点 = `MahjongGameplayView.CompleteEliminationAnimation()` 首行（每消除一对牌 +1）。2026-10-14 已补。
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

## 用户手工排布的 prefab（不要动结构）

- `TxWithdrawBar.prefab` / `TxHudSliders.prefab` — 用户改过，只改脚本不碰 prefab。

## guid 映射（参考 → 本项目）

| 资产 | 参考 guid | 本项目 guid |
|---|---|---|
| neuron-extrabold SDF | 9e8d8050d99c2cb49b754817ec264f9a | d6389bd164024a56ba3421ce9947bde4 |
| Atlas White Outline Material | 193391c627b444e1ac679b45c076f013 | fb89e49c0b6545c4a9f884e2dee61fd5 |
| badge.png | 3447c0dcde9e2cd45b9273286a93d0bd | e557dd68370d40ec9c6c1e3ce75654c6 |

美术类资产迁移后 guid 全变；预制体连 meta 复制 guid 不变。搬 YAML 前必须建全量 guid 重映射表。
