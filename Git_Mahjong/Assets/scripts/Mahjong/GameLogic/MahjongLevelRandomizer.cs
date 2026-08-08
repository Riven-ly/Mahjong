using System;
using System.Collections.Generic;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 根据固定牌位生成两两配对且可按既定顺序清除的随机牌面。
    /// </summary>
    public static class MahjongLevelRandomizer
    {
        /// <summary>
        /// 复制关卡布局并为每个可消除牌对随机分配不重复的牌型。
        /// </summary>
        public static MahjongLevelDefinition CreateRandomizedLevel(MahjongLevelDefinition sourceLevel, IReadOnlyList<int> availableTypeIds, Random random)
        {
            if (sourceLevel.cards.Count % MahjongConfig.MatchCount != 0)
            {
                throw new InvalidOperationException($"关卡{sourceLevel.level}的牌位数量不能两两配对。");
            }

            int pairCount = sourceLevel.cards.Count / MahjongConfig.MatchCount;
            List<int> removalOrder = GetRemovalOrder(sourceLevel, random);
            List<int> shuffledTypeIds = new List<int>(availableTypeIds);
            Shuffle(shuffledTypeIds, random);
            var cards = new List<MahjongLevelCardDefinition>(sourceLevel.cards.Count);
            for (int i = 0; i < sourceLevel.cards.Count; i++)
            {
                MahjongLevelCardDefinition sourceCard = sourceLevel.cards[i];
                cards.Add(new MahjongLevelCardDefinition
                {
                    typeId = 0,
                    layer = sourceCard.layer,
                    coordY = sourceCard.coordY,
                    coordX = sourceCard.coordX
                });
            }

            for (int pairIndex = 0; pairIndex < pairCount; pairIndex += MahjongConfig.MatchCount)
            {
                if (pairIndex > 0 && pairIndex % shuffledTypeIds.Count == 0)
                {
                    Shuffle(shuffledTypeIds, random);
                }

                int firstTypeId = shuffledTypeIds[pairIndex % shuffledTypeIds.Count];
                if (pairIndex + 1 >= pairCount)
                {
                    cards[removalOrder[pairIndex * MahjongConfig.MatchCount]].typeId = firstTypeId;
                    cards[removalOrder[pairIndex * MahjongConfig.MatchCount + 1]].typeId = firstTypeId;
                    continue;
                }

                int secondTypeId = shuffledTypeIds[(pairIndex + 1) % shuffledTypeIds.Count];
                int firstOrderIndex = pairIndex * MahjongConfig.MatchCount;
                cards[removalOrder[firstOrderIndex]].typeId = firstTypeId;
                cards[removalOrder[firstOrderIndex + 1]].typeId = secondTypeId;
                cards[removalOrder[firstOrderIndex + 2]].typeId = firstTypeId;
                cards[removalOrder[firstOrderIndex + 3]].typeId = secondTypeId;
            }

            return new MahjongLevelDefinition
            {
                level = sourceLevel.level,
                gridColumnCount = sourceLevel.gridColumnCount,
                gridRowCount = sourceLevel.gridRowCount,
                randomizeTypeIds = sourceLevel.randomizeTypeIds,
                cards = cards
            };
        }

        /// <summary>
        /// 按当前遮挡与左右阻挡规则生成一条单调可行的取牌顺序。
        /// </summary>
        private static List<int> GetRemovalOrder(MahjongLevelDefinition levelDefinition, Random random)
        {
            var cardsOnBoard = new bool[levelDefinition.cards.Count];
            for (int i = 0; i < cardsOnBoard.Length; i++)
            {
                cardsOnBoard[i] = true;
            }

            var removalOrder = new List<int>(levelDefinition.cards.Count);
            while (removalOrder.Count < levelDefinition.cards.Count)
            {
                List<int> selectableIndexes = GetSelectableCardIndexes(levelDefinition, cardsOnBoard);
                if (selectableIndexes.Count == 0)
                {
                    throw new InvalidOperationException($"关卡{levelDefinition.level}不存在可清除的牌位顺序。");
                }

                int selectableIndex = selectableIndexes[random.Next(selectableIndexes.Count)];
                cardsOnBoard[selectableIndex] = false;
                removalOrder.Add(selectableIndex);
            }

            return removalOrder;
        }

        /// <summary>
        /// 获取当前牌面中全部无遮挡的卡牌索引。
        /// </summary>
        private static List<int> GetSelectableCardIndexes(MahjongLevelDefinition levelDefinition, bool[] cardsOnBoard)
        {
            var selectableIndexes = new List<int>();
            for (int cardIndex = 0; cardIndex < levelDefinition.cards.Count; cardIndex++)
            {
                if (cardsOnBoard[cardIndex] && !IsCovered(levelDefinition, cardsOnBoard, cardIndex))
                {
                    selectableIndexes.Add(cardIndex);
                }
            }

            return selectableIndexes;
        }

        /// <summary>
        /// 判断指定卡牌是否被仍在牌面上的更高层卡牌遮挡。
        /// </summary>
        private static bool IsCovered(MahjongLevelDefinition levelDefinition, bool[] cardsOnBoard, int cardIndex)
        {
            MahjongLevelCardDefinition card = levelDefinition.cards[cardIndex];
            for (int otherIndex = 0; otherIndex < levelDefinition.cards.Count; otherIndex++)
            {
                if (!cardsOnBoard[otherIndex] || otherIndex == cardIndex)
                {
                    continue;
                }

                MahjongLevelCardDefinition other = levelDefinition.cards[otherIndex];
                if (other.layer > card.layer && MahjongLayoutGeometry.HasAreaOverlap(card, other))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定卡牌是否同时存在同层同行的左右相邻卡牌。
        /// </summary>
        private static bool HasBothSideNeighbors(MahjongLevelDefinition levelDefinition, bool[] cardsOnBoard, int cardIndex)
        {
            MahjongLevelCardDefinition card = levelDefinition.cards[cardIndex];
            int cardCenterColumn = MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card);
            int cardCenterRow = MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card);
            bool hasLeft = false;
            bool hasRight = false;
            for (int otherIndex = 0; otherIndex < levelDefinition.cards.Count; otherIndex++)
            {
                if (!cardsOnBoard[otherIndex] || otherIndex == cardIndex)
                {
                    continue;
                }

                MahjongLevelCardDefinition other = levelDefinition.cards[otherIndex];
                if (other.layer != card.layer || MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(other) != cardCenterRow)
                {
                    continue;
                }

                int otherCenterColumn = MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(other);
                hasLeft |= otherCenterColumn == cardCenterColumn - MahjongLayoutGeometry.HalfGridUnitsPerCell;
                hasRight |= otherCenterColumn == cardCenterColumn + MahjongLayoutGeometry.HalfGridUnitsPerCell;
            }

            return hasLeft && hasRight;
        }

        /// <summary>
        /// 原地随机打乱牌型列表。
        /// </summary>
        private static void Shuffle(List<int> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }
    }
}
