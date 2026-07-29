#if false
using System;
using System.Collections.Generic;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将关卡配置合法性校验器。
    /// </summary>
    public static class MahjongLevelValidator
    {
        /// <summary>
        /// 校验单个关卡的规则、牌型数量与固定布局。配置无效时抛出包含关卡信息的异常。
        /// </summary>
        public static void Validate(MahjongLevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            if (levelDefinition.level <= 0)
            {
                throw new InvalidOperationException("关卡编号必须大于零。");
            }

            if (levelDefinition.gridColumnCount <= 0 || levelDefinition.gridRowCount <= 0)
            {
                throw new InvalidOperationException($"关卡{levelDefinition.level}的网格行列必须大于零。");
            }

            if (levelDefinition.cards == null || levelDefinition.cards.Count == 0)
            {
                throw new InvalidOperationException($"关卡{levelDefinition.level}未配置卡牌布局。");
            }

            int maxColumn = levelDefinition.gridColumnCount - 1;
            int maxRow = levelDefinition.gridRowCount - 1;
            var typeCounts = new Dictionary<int, int>();
            var occupiedPositions = new HashSet<string>();
            for (int i = 0; i < levelDefinition.cards.Count; i++)
            {
                MahjongLevelCardDefinition card = levelDefinition.cards[i];
                if (card == null || card.typeId <= 0)
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}的第{i}张牌使用了无效类型。");
                }

                if (card.layer < 0)
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}的第{i}张牌层级无效。");
                }

                if (card.column < 0 || card.column > maxColumn || card.row < 0 || card.row > maxRow)
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}的第{i}张牌坐标越界。");
                }

                string positionKey = $"{card.layer}:{card.column}:{card.row}";
                if (!occupiedPositions.Add(positionKey))
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}在{positionKey}存在重复卡牌。");
                }

                typeCounts.TryGetValue(card.typeId, out int typeCount);
                typeCounts[card.typeId] = typeCount + 1;
            }

            foreach (KeyValuePair<int, int> pair in typeCounts)
            {
                if (pair.Value % MahjongConfig.MatchCount != 0)
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}的类型{pair.Key}数量不能被消除数量整除。");
                }
            }
        }
    }
}
#endif
