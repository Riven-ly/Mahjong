Mahjong Blast 四色碎片补全包
蓝、绿、粉、黄四种原始碎片，每色 Style1、Style2 两种原包喷发样式，共 8 个独立预制件。
打开 Demo/FragmentsDemo.unity 或将 Prefabs/FragmentPreview_WithCanvas.prefab 拖入空场景，Play 播放，空格重播。
每套保留原始 6 个粒子系统、双侧碎片喷发、贴图动画、速度、旋转、渐变和时序。四种颜色分别引用原包的 Blue_28、Green_28、Pink_28、Yellow_22 材质与原图，未通过简单染色替代。
上次 Blue/Orange 包只有光圈配色不同，碎片共用蓝白图。本包提供实际的四色碎片。
兼容项：Android Shader 改为可编辑 Blend/Additive；原性能分档控制脚本的方法体未恢复，使用预制件保存的粒子参数，不自动按设备性能调整。对应原始 nodeRules/particleRules 已附于 SourceQualityRules.json，不影响静态配色区分。
依赖：Coffee UIParticle 4.13.0 (MIT)，已内置；已有该插件可取消 ThirdParty/UIParticle 导入。
视觉已做本地播放检查，但不宣称与手机运行时逐帧完全一致。
