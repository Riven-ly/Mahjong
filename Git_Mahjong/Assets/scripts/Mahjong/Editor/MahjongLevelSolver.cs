using System;
using System.Collections.Generic;
using System.Text;
using MahjongGame.GameLogic;
using MahjongGame.Model;

namespace MahjongGame.EditorTools
{
    /// <summary>
    /// 编辑器关卡配置可解性搜索器。
    /// </summary>
    public static class MahjongLevelSolver
    {
        /// <summary>
        /// 搜索指定关卡的完整通关路径。调用前必须完成关卡结构校验。
        /// </summary>
        public static bool TrySolve(MahjongLevelDefinition levelDefinition, out List<int> solutionCardIndexes)
        {
            if (levelDefinition == null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            bool[] cardsOnBoard = new bool[levelDefinition.cards.Count];
            for (int i = 0; i < cardsOnBoard.Length; i++)
            {
                cardsOnBoard[i] = true;
            }

            solutionCardIndexes = new List<int>(levelDefinition.cards.Count);
            var slotTypeIds = new List<int>(MahjongConfig.SlotCapacity);
            var failedStates = new HashSet<string>();
            return Search(levelDefinition, cardsOnBoard, cardsOnBoard.Length, slotTypeIds, solutionCardIndexes, failedStates);
        }

        /// <summary>
        /// 递归搜索当前牌面与卡槽状态。失败状态会被缓存以避免重复搜索。
        /// </summary>
        private static bool Search(
            MahjongLevelDefinition levelDefinition,
            bool[] cardsOnBoard,
            int remainingCardCount,
            List<int> slotTypeIds,
            List<int> solutionCardIndexes,
            HashSet<string> failedStates)
        {
            if (remainingCardCount == 0)
            {
                return slotTypeIds.Count == 0;
            }

            if (slotTypeIds.Count >= MahjongConfig.SlotCapacity)
            {
                return false;
            }

            string stateKey = CreateStateKey(cardsOnBoard, slotTypeIds);
            if (!failedStates.Add(stateKey))
            {
                return false;
            }

            List<int> selectableCardIndexes = GetSelectableCardIndexes(levelDefinition, cardsOnBoard);
            selectableCardIndexes.Sort((leftIndex, rightIndex) =>
            {
                bool leftMatchesSlot = slotTypeIds.Contains(levelDefinition.cards[leftIndex].typeId);
                bool rightMatchesSlot = slotTypeIds.Contains(levelDefinition.cards[rightIndex].typeId);
                return rightMatchesSlot.CompareTo(leftMatchesSlot);
            });

            for (int i = 0; i < selectableCardIndexes.Count; i++)
            {
                int cardIndex = selectableCardIndexes[i];
                int typeId = levelDefinition.cards[cardIndex].typeId;
                cardsOnBoard[cardIndex] = false;
                slotTypeIds.Add(typeId);
                bool matched = RemoveMatch(slotTypeIds, typeId);
                solutionCardIndexes.Add(cardIndex);

                if (Search(levelDefinition, cardsOnBoard, remainingCardCount - 1, slotTypeIds, solutionCardIndexes, failedStates))
                {
                    return true;
                }

                solutionCardIndexes.RemoveAt(solutionCardIndexes.Count - 1);
                if (matched)
                {
                    slotTypeIds.Add(typeId);
                }
                else
                {
                    slotTypeIds.RemoveAt(slotTypeIds.Count - 1);
                }
                cardsOnBoard[cardIndex] = true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前状态下全部无遮挡且未被左右同时夹住的卡牌索引。
        /// </summary>
        private static List<int> GetSelectableCardIndexes(MahjongLevelDefinition levelDefinition, bool[] cardsOnBoard)
        {
            var selectableCardIndexes = new List<int>();
            for (int cardIndex = 0; cardIndex < levelDefinition.cards.Count; cardIndex++)
            {
                if (!cardsOnBoard[cardIndex] || IsCovered(levelDefinition, cardsOnBoard, cardIndex) || HasBothSideNeighbors(levelDefinition, cardsOnBoard, cardIndex))
                {
                    continue;
                }

                selectableCardIndexes.Add(cardIndex);
            }

            return selectableCardIndexes;
        }

        /// <summary>
        /// 判断指定卡牌是否被当前牌面中的更高层卡牌覆盖。
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
                if (other.layer > card.layer &&
                    MahjongLayoutGeometry.HasAreaOverlap(
                        card.column,
                        card.row,
                        card.layer,
                        other.column,
                        other.row,
                        other.layer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定卡牌在同层同行是否同时存在左右紧邻牌。
        /// </summary>
        private static bool HasBothSideNeighbors(MahjongLevelDefinition levelDefinition, bool[] cardsOnBoard, int cardIndex)
        {
            MahjongLevelCardDefinition card = levelDefinition.cards[cardIndex];
            bool hasLeft = false;
            bool hasRight = false;
            for (int otherIndex = 0; otherIndex < levelDefinition.cards.Count; otherIndex++)
            {
                if (!cardsOnBoard[otherIndex] || otherIndex == cardIndex)
                {
                    continue;
                }

                MahjongLevelCardDefinition other = levelDefinition.cards[otherIndex];
                if (other.layer != card.layer || other.row != card.row)
                {
                    continue;
                }

                if (other.column == card.column - MahjongConfig.GridCoordinateStep)
                {
                    hasLeft = true;
                }
                else if (other.column == card.column + MahjongConfig.GridCoordinateStep)
                {
                    hasRight = true;
                }
            }

            return hasLeft && hasRight;
        }

        /// <summary>
        /// 检查卡槽中的指定类型是否达到消除数量，达到时移除对应卡牌。
        /// </summary>
        private static bool RemoveMatch(List<int> slotTypeIds, int typeId)
        {
            int matchCount = 0;
            for (int i = 0; i < slotTypeIds.Count; i++)
            {
                if (slotTypeIds[i] == typeId)
                {
                    matchCount++;
                }
            }

            if (matchCount < MahjongConfig.MatchCount)
            {
                return false;
            }

            for (int i = slotTypeIds.Count - 1; i >= 0 && matchCount > 0; i--)
            {
                if (slotTypeIds[i] == typeId)
                {
                    slotTypeIds.RemoveAt(i);
                    matchCount--;
                }
            }

            return true;
        }

        /// <summary>
        /// 创建用于失败状态缓存的稳定键值。
        /// </summary>
        private static string CreateStateKey(bool[] cardsOnBoard, List<int> slotTypeIds)
        {
            var builder = new StringBuilder(cardsOnBoard.Length + slotTypeIds.Count * 4 + 1);
            for (int i = 0; i < cardsOnBoard.Length; i++)
            {
                builder.Append(cardsOnBoard[i] ? '1' : '0');
            }

            builder.Append('|');
            int[] sortedSlotTypeIds = slotTypeIds.ToArray();
            Array.Sort(sortedSlotTypeIds);
            for (int i = 0; i < sortedSlotTypeIds.Length; i++)
            {
                builder.Append(sortedSlotTypeIds[i]).Append(',');
            }

            return builder.ToString();
        }
    }
}
