---
id: kd_builtin_memory_user_preference
injectMode: rule
aiEditMode: auto
maintenanceRules: |-
  - Record only long-term user preferences that stay stable across tasks
  - Prioritize language, reporting style, code style, taboos, and explicit requirements
  - Keep each entry short and limited to stable preferences or hard constraints
  - Keep the list within 20 items and merge similar preferences
  - Remove one-off arrangements, temporary phrasing, and unconfirmed inferences
---

- 项目新增或修改的代码注释与 XML 注释全部使用中文。
- 新增或修改的 `class`、`struct`、`enum` 声明均需添加中文 XML 类型说明。
- Model 中承载数据的字段、属性、配置常量及只读配置集合需在声明行末添加中文 `//` 注释。
- 新增或修改的所有方法（包括公开、私有、构造函数和 Unity 生命周期函数）均需添加中文 XML 注释。
- 修改项目内容后不执行编译、自动验证或测试；由用户自行校验。
