Cash Rewards Combo Effects — 使用说明

内容：COMBO / PERFECT / EXCELLENT 三种文字、七种表情、两段原音效、静态 UGUI 预制件、演示场景。

推荐版本：Unity 2022.3 LTS。项目需要 UGUI 1.0.0 与 TextMeshPro 3.0.7（Window > Package Manager）。Spine 资源版本为 4.2。

选包：
1. 没有 Spine 的项目：导入 CashRewardsComboEffects-WithSpine.unitypackage，已包含官方 Spine 4.2 运行时源码及编辑器支持，不使用原包 DLL。
2. 已装官方 Spine 4.2 源码的项目：导入 CashRewardsComboEffects-ExistingSpine.unitypackage，避免重复安装 DLL。包内 Editor 修复器会将旧 DLL 的脚本引用绑定至现有 Spine 类；也可执行 Tools > CashRewards Combo Effects > Repair Spine Bindings。
3. 不同时导入两种包。不兼容的 Spine 3.x / 4.0 / 4.1 不能直接读取这套 4.2 动画。

快速试用：
打开 Assets/CashRewardsComboEffects/Demo/ComboEffectsDemo.unity，点击 Play。
场景自动循环 3、5、10、15、20、30、40、100 连击，也可点击八个静态测试按钮。

接入游戏：
把 Assets/CashRewardsComboEffects/Prefabs/ComboEffects.prefab 拖到自己的 Canvas 下。
在自己的脚本中引用 CashRewards.ComboEffects.ComboEffectPlayer：

[SerializeField] private CashRewards.ComboEffects.ComboEffectPlayer comboEffects;
// 成功配对时传入你自己的连击数：
comboEffects.PlayCombo(comboCount);
// 单独播放：
comboEffects.PlayWord(CashRewards.ComboEffects.ComboEffectPlayer.Word.Perfect);
comboEffects.PlayEmoji(CashRewards.ComboEffects.ComboEffectPlayer.Emoji.StarEyes);
// 取消当前效果：
comboEffects.Hide();

效果不维护玩家连击数，不处理关卡、广告、提现或金币奖励。

静态 UI：所有 SkeletonGraphic 与数字 TextMeshPro 对象都已保存在 prefab 中。可直接修改子对象的位置、大小、材质。
播放时读取 prefab 初始位置，仅叠加原效果的上浮/数字弹跳，不运行旧项目的 UI 生成器或重排你的 Canvas。
整体大小优先修改 ComboEffects 根节点的 Scale；数字间距与上浮幅度可在组件 Inspector 调整。
不播放音效：取消组件 Play Sound。使用 unscaledDeltaTime，Time.timeScale=0 时仍可播放。

原规则：
1–2 无效果；3 起 Combo；5/10 Perfect；15/20 与30起整十连击 Excellent。
表情：5笑脸，10星星眼，15夸赞，20亲吻，30/50/70/90拍手，40/60/80惊讶，100起整十墨镜。
其余连击没有额外表情。

依赖路径：
Assets/CashRewardsComboEffects/  包含可复用脚本、静态 prefab 与 Demo。
Assets/Resources/mahjongcore/art/effects/  保留原动画资源 GUID 与相对关系。
Assets/Resources/mahjongcore/audio/combo/  音效。
字体、材质、着色器及 Spine 运行时源码 由完整包一并导出；不含原游戏业务脚本。

限制：不保证所有 Unity/Spine 版本兼容；请以本次附带验证报告为准。原素材的权利归属不因导出而改变。


新增消除特效：
Prefabs/MatchCombinedEffect.prefab：光芒 + 冲击环 + 火花 + 桃花（可选牌面碎片）。
Prefabs/MatchLightEffect.prefab：仅光芒、冲击环和火花。
Prefabs/PeachBlossomEffect.prefab：仅原 mj_piaohua 桃花动画。
Demo/MatchEffectsDemo.unity：三个静态测试按钮分别播放上述效果。

using CashRewards.ComboEffects;
[SerializeField] MatchEffectPlayer matchEffect;
matchEffect.Play();                         // 在预制件中心播放
matchEffect.PlayAt(localPoint);             // 使用 effectParent（默认预制件根节点）的局部坐标
matchEffect.PlayWithFaces(spriteA,spriteB);  // 使用自己的两张牌面生成碎片

可在 Inspector 中改 Tile Width、Include Tile Shards。光芒/桃花原参数保留；不依赖 DOTween、不需要游戏控制器。

完整包已自带数字和演示用的 SDF 字体图集。若目标项目没有 TMP Settings，包内编辑器脚本会创建必要设置；已有设置不替换。
