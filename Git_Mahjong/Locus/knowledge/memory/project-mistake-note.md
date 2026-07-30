---
id: kd_builtin_memory_project_mistake_note
type: memory
path: project-mistake-note.md
title: project-mistake-note
injectMode: full
summaryEnabled: false
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1785134626594
updatedAt: 1785392390457
---

# project-mistake-note

<!-- locus:maintain-rules:start -->
- Record only verified problems, rework causes, and avoidance steps
- Prioritize recurring pitfalls, constraints, regression points, and confirmed fixes
- Keep each entry short and focused on one lesson or constraint
- Keep the list within 20 items and merge duplicates regularly
- Remove outdated issues, non-reproducible issues, and unsupported guesses
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
- UGUI 对象跨不同 RectTransform 父节点播放移动动画时，先用 `SetParent(parent, true)` 保持世界位置，再补间目标局部坐标；使用 `false` 会在动画开始前产生位置跳变。
- 堆叠卡牌的阻挡态不能直接关闭 `blocksRaycasts`，否则点击会穿透到下层卡牌；需要拆分点击与拖拽权限，让阻挡牌接收点击反馈但禁止拖拽。
- 并行编辑同一 C# 文件可能导致部分替换未落盘；涉及相互依赖的代码块应串行写入，并在修改后重新读取调用点与初始化点。
<!-- locus:body:end -->
