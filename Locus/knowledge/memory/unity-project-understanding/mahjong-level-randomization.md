---
id: kd_8114b9a9-1c56-4a2d-9d17-2c261ca2889c
injectMode: inherit
summary: 外部麻将布局坐标导入与运行时随机配对牌型实现记录。
aiEditMode: inherit
---

- 关卡配置使用逐牌半格坐标：坐标间隔2为完整牌距，偶数坐标位于相邻奇数坐标的中点；不能按层或按坐标取整。字段采用外部表语义：`coordY` 对应横向，`coordX` 对应纵向；在项目中已存为游戏向上的坐标值，编辑器显示与鼠标输入保持相同方向。层数组顺序映射为项目 layer 0 起逐层递增。
- `MahjongLevelCardDefinition` 已收敛为唯一格式：`layer`、`coordX`、`coordY`。旧 `column`、`row`、`useCenterHalfCoordinates`、`centerColumnHalf`、`centerRowHalf` 及按奇偶层自动偏移的兼容逻辑已删除；运行时、遮挡、随机配对、编辑器和求解器均直接使用精确半格坐标。
- `Assets/scripts/Mahjong/Editor/MahjongLevelEditorWindow.cs` 默认显示和编辑当前层完整格网格；`Half Grid` 按钮切换为当前层半格中心网格，此时可编辑中点位置，关闭即恢复完整格。显示与鼠标输入均将半格纵坐标反转为“上大下小”，与 UGUI 游戏内位置和外部坐标转换保持一致。编辑器校验和求解器仅根据精确矩形重叠判定遮挡，不再施加左右夹牌限制。
- `Assets/Resources/Mahjong/Levels.json` 已从 `D:/Backup/Downloads/local_maps_zyGame_ccon.json` 导入原表第1–321关，排除尾部999附加配置。每关的 `gridColumnCount/gridRowCount` 根据外部坐标跨度推导；外部 `coordX` 在导入时反转为项目向上坐标，`coordY` 平移至从0起。
- 原表22关为奇数牌位，其中999已排除；其余21关均在原最高层+1的新层、整体正中心新增一张牌位以补为偶数。当前目录321关均为偶数张且无同层重复坐标。
- `MahjongGameplayView` 不再按网格尺寸使用固定缩放表，而是依据 `Adaptive_point1`（上边界）、`Adaptive_point2`（下边界）和当前 `Screen.safeArea` 自动缩放 BoardRoot：逐关计算全部牌的完整矩形边缘（171×199），并取各边界允许缩放中的较小值，确保最上牌上边缘不越过Point1、最下牌下边缘不越过Point2，最左/最右牌完整边缘不越出安全区且各保留50像素。两个上下锚点已绑定在 `Assets/Resources/UI/Panel/GameScenePanel.prefab` 的 MahjongGameplayView。
- `MahjongLevelDefinition.randomizeTypeIds` 控制进入关卡时是否为固定牌位生成随机牌面。`MahjongLevelRandomizer` 优先基于遮挡规则找出成对可取顺序，再沿该顺序连续按两张写入同一随机牌型（A,A,B,B）。第5关等布局若在中途只露出一张牌，随机器返回空；`MahjongLevelCatalogLoader` 静默使用 JSON 原牌型，不回退单张顺序、不抛异常。牌对数量超过42时重新洗牌循环，允许同一牌型在一局中出现多对；双选玩法的当前可操作配对由 `EnsurePlayablePair` 后台洗牌保障。
- 已编译并检查导入结果；321关均无重复坐标，抽检第1、41、222、247、321关均可生成配对牌面。
