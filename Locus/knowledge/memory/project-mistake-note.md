---
id: kd_builtin_memory_project_mistake_note
injectMode: full
aiEditMode: auto
maintenanceRules: |-
  - Record only verified problems, rework causes, and avoidance steps
  - Prioritize recurring pitfalls, constraints, regression points, and confirmed fixes
  - Keep each entry short and focused on one lesson or constraint
  - Keep the list within 20 items and merge duplicates regularly
  - Remove outdated issues, non-reproducible issues, and unsupported guesses
---

- UGUI 对象跨不同 RectTransform 父节点播放移动动画时，先用 `SetParent(parent, true)` 保持世界位置，再补间目标局部坐标；使用 `false` 会在动画开始前产生位置跳变。
- 堆叠卡牌的阻挡态不能直接关闭 `blocksRaycasts`，否则点击会穿透到下层卡牌；需要拆分点击与拖拽权限，让阻挡牌接收点击反馈但禁止拖拽。
- 并行编辑同一 C# 文件可能导致部分替换未落盘；涉及相互依赖的代码块应串行写入，并在修改后重新读取调用点与初始化点。
- MahjongCell 入槽缩放补间被其他操作 Kill 后，不能只重建位置补间；需要在所有卡槽重排/重定向补间中同时重建到 `MahjongViewConfig.SlotCardScale` 的缩放补间，并在完成回调中钳定最终局部缩放。
