using System;
using System.Collections.Generic;
using MahjongGame.GameLogic;
using MahjongGame.Model;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    /// <summary>
    /// 支持 Unity Undo 的麻将关卡编辑器临时状态。
    /// </summary>
    public sealed class MahjongLevelEditorState : ScriptableObject
    {
        public int originalLevel; // 当前加载关卡的原始编号，新关卡为零
        public int level = 1; // 正在编辑的关卡编号
        public int gridColumnCount = 5; // 正在编辑的网格列数
        public int gridRowCount = 5; // 正在编辑的网格行数
        public bool randomizeTypeIds; // 是否在进入关卡时随机生成牌型
        public int currentLayer; // 当前编辑层级
        public int selectedTypeId = 1; // 当前画笔使用的卡牌类型ID
        public List<int> cardTypeIds = new List<int>(); // 编辑器卡牌类型面板
        public List<MahjongLevelCardDefinition> cards = new List<MahjongLevelCardDefinition>(); // 正在编辑的固定卡牌布局

        /// <summary>
        /// 从关卡目录与指定关卡创建可编辑副本。
        /// </summary>
        public void Load(MahjongLevelCatalog catalog, MahjongLevelDefinition levelDefinition)
        {
            originalLevel = levelDefinition.level;
            level = levelDefinition.level;
            gridColumnCount = levelDefinition.gridColumnCount;
            gridRowCount = levelDefinition.gridRowCount;
            randomizeTypeIds = levelDefinition.randomizeTypeIds;
            currentLayer = 0;
            cardTypeIds = new List<int>(catalog.cardTypeIds);
            cards = CloneCards(levelDefinition.cards);
            EnsureSelectedType();
        }

        /// <summary>
        /// 使用目录牌型和指定编号创建空白关卡。
        /// </summary>
        public void CreateNew(MahjongLevelCatalog catalog, int newLevel)
        {
            originalLevel = 0;
            level = newLevel;
            gridColumnCount = 4;
            gridRowCount = 4;
            randomizeTypeIds = false;
            currentLayer = 0;
            cardTypeIds = new List<int>(catalog.cardTypeIds);
            cards.Clear();
            EnsureSelectedType();
        }

        /// <summary>
        /// 获取当前布局的可编辑层数；空布局至少返回一层。
        /// </summary>
        public int GetLayerCount()
        {
            int maxLayer = currentLayer;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].layer > maxLayer)
                {
                    maxLayer = cards[i].layer;
                }
            }

            return maxLayer + 1;
        }

        /// <summary>
        /// 查找指定层级和精确半格中心坐标中的卡牌；不存在时返回空。
        /// </summary>
        public MahjongLevelCardDefinition GetCard(int layer, int coordY, int coordX)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                MahjongLevelCardDefinition card = cards[i];
                if (card.layer == layer &&
                    MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card) == coordY &&
                    MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card) == coordX)
                {
                    return card;
                }
            }

            return null;
        }

        /// <summary>
        /// 在指定层级和精确半格中心坐标放置或替换卡牌。
        /// </summary>
        public void SetCard(int layer, int coordY, int coordX, int typeId)
        {
            MahjongLevelCardDefinition card = GetCard(layer, coordY, coordX);
            if (card == null)
            {
                cards.Add(new MahjongLevelCardDefinition
                {
                    typeId = typeId,
                    layer = layer,
                    coordY = coordY,
                    coordX = coordX
                });
                return;
            }

            card.typeId = typeId;
        }

        /// <summary>
        /// 删除指定层级和精确半格中心坐标中的卡牌。
        /// </summary>
        public void RemoveCard(int layer, int coordY, int coordX)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                MahjongLevelCardDefinition card = cards[i];
                if (card.layer == layer &&
                    MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card) == coordY &&
                    MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card) == coordX)
                {
                    cards.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 清空指定层级中的全部卡牌。
        /// </summary>
        public void ClearLayer(int layer)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i].layer == layer)
                {
                    cards.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 删除最高层及该层全部卡牌，并切换到新的最高层。
        /// </summary>
        public void DeleteHighestLayer()
        {
            int highestLayer = GetLayerCount() - 1;
            if (highestLayer <= 0)
            {
                ClearLayer(0);
                currentLayer = 0;
                return;
            }

            ClearLayer(highestLayer);
            currentLayer = highestLayer - 1;
        }

        /// <summary>
        /// 删除网格范围外的卡牌，用于用户确认后的缩小网格操作。
        /// </summary>
        public void CropOutsideGrid()
        {
            int maximumColumnHalf = (gridColumnCount - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell;
            int maximumRowHalf = (gridRowCount - 1) * MahjongLayoutGeometry.HalfGridUnitsPerCell;
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                MahjongLevelCardDefinition card = cards[i];
                int coordY = MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card);
                int coordX = MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card);
                if (coordY < 0 || coordY > maximumColumnHalf ||
                    coordX < 0 || coordX > maximumRowHalf)
                {
                    cards.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 创建当前编辑关卡的独立保存副本。
        /// </summary>
        public MahjongLevelDefinition CreateLevelDefinition()
        {
            return new MahjongLevelDefinition
            {
                level = level,
                gridColumnCount = gridColumnCount,
                gridRowCount = gridRowCount,
                randomizeTypeIds = randomizeTypeIds,
                cards = CloneCards(cards)
            };
        }

        /// <summary>
        /// 确保当前选中牌型存在于牌型面板中。
        /// </summary>
        private void EnsureSelectedType()
        {
            if (cardTypeIds.Count == 0)
            {
                selectedTypeId = 0;
                return;
            }

            if (!cardTypeIds.Contains(selectedTypeId))
            {
                selectedTypeId = cardTypeIds[0];
            }
        }

        /// <summary>
        /// 深拷贝卡牌布局列表。
        /// </summary>
        private static List<MahjongLevelCardDefinition> CloneCards(List<MahjongLevelCardDefinition> sourceCards)
        {
            var clonedCards = new List<MahjongLevelCardDefinition>(sourceCards.Count);
            for (int i = 0; i < sourceCards.Count; i++)
            {
                MahjongLevelCardDefinition card = sourceCards[i];
                clonedCards.Add(new MahjongLevelCardDefinition
                {
                    typeId = card.typeId,
                    layer = card.layer,
                    coordY = MahjongLayoutGeometry.GetCenterColumnInHalfGridUnits(card),
                    coordX = MahjongLayoutGeometry.GetCenterRowInHalfGridUnits(card)
                });
            }

            return clonedCards;
        }
    }
}
