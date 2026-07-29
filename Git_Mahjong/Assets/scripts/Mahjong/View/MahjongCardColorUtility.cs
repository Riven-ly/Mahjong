using UnityEngine;

namespace MahjongGame.View
{
    /// <summary>
    /// 为卡牌类型生成编辑器与运行时一致的稳定占位颜色。
    /// </summary>
    public static class MahjongCardColorUtility
    {
        /// <summary>
        /// 根据正整数卡牌类型ID生成稳定颜色。
        /// </summary>
        public static Color GetColor(int typeId)
        {
            float hue = Mathf.Repeat(typeId * 0.61803398875f, 1f);
            return Color.HSVToRGB(hue, 0.55f, 0.95f);
        }
    }
}
