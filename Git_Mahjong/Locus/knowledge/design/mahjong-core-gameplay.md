---
id: kd_bb502085-db65-4508-9b3d-235bf18cf985
type: design
path: mahjong-core-gameplay.md
title: mahjong-core-gameplay
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785138573703
updatedAt: 1785393778233
---

# mahjong-core-gameplay

## Summary
单机《羊了个羊》式主玩法设计与分层开发约束。

## Content
## 核心规则
- 卡牌按关卡配置进行多层固定堆叠，上层遮挡下层，仅完全无遮挡卡牌可操作。
- 有效卡牌进入底部卡槽；2张同类型卡牌自动消除。
- 卡槽容量为4；卡槽填满且无法消除时失败，牌面清空时胜利。
- 选中卡牌同层、同行的左右紧邻格同时存在未移除卡牌时，禁止进入卡槽。
- 支持重新开局；预留洗牌与撤销接口，暂不实现道具。

## 分层约束
- 单向依赖：Model → GameLogic → View。
- Model 为纯数据，不使用 Unity API。
- GameLogic 负责布局、遮挡、左右邻牌、卡槽、消除和胜负。
- View 仅使用 UGUI，负责显示、动画和事件，不直接修改逻辑数据。
- 复用现有 UI 框架，主玩法集成到 GameScenePanel。
- MahjongCell 使用 Image + 数字占位，并实现拖拽与点击接口。
- 配对消除在卡牌缩放消失结束时，于原卡槽位置播放独立、可池化的白色麻将碎片粒子；效果由主碎片层和延迟残屑层组成，表现为持续喷发、右上扩散、重力下落和末段淡出，单次完整播放约 1.8 秒。

## 关卡配置
- 关卡目录使用单个 JSON 文件，按 `GameManager.Instance.playerInfo.level` 读取对应关卡。
- 每关仅配置标准整数网格行列，以及每张牌固定的类型、层级和坐标；偶数层的 `column`、`row` 必须在 `0..gridColumnCount-1` / `0..gridRowCount-1`，奇数层允许额外的左侧与底部扩展带，最小值为 `-1`。
- 分层布局采用羊了个羊式错位：偶数层保持整格，奇数层相对基础网格向右上横纵各偏移半格；高层卡牌与下层卡牌存在面积重叠时构成遮挡。
- 全局最大层数为10层，允许 Layer 0–9；堆叠层数、启用牌型和每种牌数量均从卡牌列表自动推导。
- 固定布局允许网格内留白和不连续轮廓，但整体保持紧凑；同类型牌应分散，避免同层相邻成对摆放。
- 关卡结构、边界、数量、重复位置和可解性全部只在编辑器生成或导入配置时校验；运行时只反序列化、按等级读取并创建卡牌，不执行任何关卡配置校验。
- 找不到玩家等级对应关卡时，从已配置关卡中随机选择一关。
- 卡槽容量4、匹配消除数量2为全局固定规则。
