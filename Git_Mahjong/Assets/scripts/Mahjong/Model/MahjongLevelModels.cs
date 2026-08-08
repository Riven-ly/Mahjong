using System;
using System.Collections.Generic;

namespace MahjongGame.Model
{
    /// <summary>
    /// 全部麻将关卡配置的纯数据目录。
    /// </summary>
    [Serializable]
    public sealed class MahjongLevelCatalog
    {
        public int version; // 关卡目录数据版本
        public List<int> cardTypeIds = new List<int>(); // 关卡编辑器可用的全部卡牌类型ID
        public List<MahjongLevelDefinition> levels = new List<MahjongLevelDefinition>(); // 全部关卡配置
    }

    /// <summary>
    /// 单个麻将关卡的规则与固定布局配置。
    /// </summary>
    [Serializable]
    public sealed class MahjongLevelDefinition
    {
        public int level; // 对应玩家等级的关卡编号
        public int gridColumnCount; // 逻辑网格列数
        public int gridRowCount; // 逻辑网格行数
        public bool randomizeTypeIds; // 是否在每次开局时随机生成两两配对且可解的牌型
        public List<MahjongLevelCardDefinition> cards = new List<MahjongLevelCardDefinition>(); // 本关全部固定卡牌布局

        /// <summary>
        /// 判断指定卡牌类型是否存在于当前关卡布局中。
        /// </summary>
        public bool ContainsCardType(int typeId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].typeId == typeId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 根据卡牌布局推导堆叠层数。调用前必须保证卡牌列表非空。
        /// </summary>
        public int GetLayerCount()
        {
            int maxLayer = 0;
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
        /// 根据类型ID在首次出现顺序中的索引生成稳定显示序号。调用前必须保证类型存在。
        /// </summary>
        public int GetCardTypeIndex(int typeId)
        {
            int typeIndex = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                int currentTypeId = cards[i].typeId;
                bool appearedBefore = false;
                for (int previousIndex = 0; previousIndex < i; previousIndex++)
                {
                    if (cards[previousIndex].typeId == currentTypeId)
                    {
                        appearedBefore = true;
                        break;
                    }
                }

                if (appearedBefore)
                {
                    continue;
                }

                if (currentTypeId == typeId)
                {
                    return typeIndex;
                }

                typeIndex++;
            }

            throw new ArgumentOutOfRangeException(nameof(typeId));
        }

        /// <summary>
        /// 统计当前关卡中不同卡牌类型的数量。
        /// </summary>
        public int GetCardTypeCount()
        {
            int typeCount = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                bool appearedBefore = false;
                for (int previousIndex = 0; previousIndex < i; previousIndex++)
                {
                    if (cards[previousIndex].typeId == cards[i].typeId)
                    {
                        appearedBefore = true;
                        break;
                    }
                }

                if (!appearedBefore)
                {
                    typeCount++;
                }
            }

            return typeCount;
        }
    }

    /// <summary>
    /// 单张关卡卡牌的固定布局配置。
    /// </summary>
    [Serializable]
    public sealed class MahjongLevelCardDefinition
    {
        public int typeId; // 卡牌类型ID
        public int layer; // 卡牌所在堆叠层级
        public int coordY; // 卡牌中心横向半格坐标
        public int coordX; // 卡牌中心纵向半格坐标
    }
}
