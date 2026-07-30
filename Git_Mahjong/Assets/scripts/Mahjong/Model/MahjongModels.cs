using System;
using System.Collections.Generic;

namespace MahjongGame.Model
{
    /// <summary>
    /// 麻将卡牌在主玩法生命周期中的状态。
    /// </summary>
    public enum MahjongCardState
    {
        OnBoard,
        Moving,
        InSlot,
        PendingElimination,
        Eliminated
    }

    /// <summary>
    /// 单局麻将主玩法的整体运行状态。
    /// </summary>
    public enum MahjongGameState
    {
        Ready,
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// 不依赖 Unity API 的麻将逻辑网格坐标。
    /// </summary>
    [Serializable]
    public readonly struct MahjongGridPosition : IEquatable<MahjongGridPosition>
    {
        public int Column { get; } // 逻辑网格列索引
        public int Row { get; } // 逻辑网格行索引

        /// <summary>
        /// 创建逻辑网格坐标。列与行必须大于或等于零。
        /// </summary>
        public MahjongGridPosition(int column, int row)
        {
            Column = column;
            Row = row;
        }

        /// <summary>
        /// 判断当前坐标与另一坐标的列和行是否相同。
        /// </summary>
        public bool Equals(MahjongGridPosition other)
        {
            return Column == other.Column && Row == other.Row;
        }

        /// <summary>
        /// 判断当前坐标是否与指定对象相等。
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is MahjongGridPosition other && Equals(other);
        }

        /// <summary>
        /// 根据列和行生成哈希值。
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }
    }

    /// <summary>
    /// 单张麻将卡牌的纯数据模型。
    /// </summary>
    [Serializable]
    public sealed class MahjongCardModel
    {
        public int InstanceId { get; } // 卡牌唯一实例ID
        public int TypeId { get; } // 卡牌类型ID
        public int Layer { get; } // 卡牌所在堆叠层级
        public MahjongGridPosition Position { get; } // 卡牌逻辑网格坐标
        public MahjongCardState State { get; private set; } // 卡牌当前状态

        /// <summary>
        /// 根据编辑器已校验的关卡数据创建单张卡牌模型。
        /// </summary>
        public MahjongCardModel(int instanceId, int typeId, int layer, MahjongGridPosition position)
        {
            InstanceId = instanceId;
            TypeId = typeId;
            Layer = layer;
            Position = position;
            State = MahjongCardState.OnBoard;
        }

        /// <summary>
        /// 修改卡牌状态。调用前必须完成本次操作的全部合法性校验。
        /// </summary>
        public void SetState(MahjongCardState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// 麻将底部卡槽的纯数据模型。
    /// </summary>
    [Serializable]
    public sealed class MahjongSlotModel
    {
        private readonly List<int> cardInstanceIds = new List<int>(MahjongConfig.SlotCapacity); // 卡槽内按顺序保存的卡牌实例ID

        public IReadOnlyList<int> CardInstanceIds => cardInstanceIds; // 卡槽内只读卡牌实例ID列表
        public int Count => cardInstanceIds.Count; // 卡槽当前卡牌数量
        public bool IsFull => Count >= MahjongConfig.SlotCapacity; // 卡槽是否已达到容量上限

        /// <summary>
        /// 将卡牌实例ID插入卡槽指定位置。调用前必须确认卡牌允许入槽、索引有效且卡槽未满。
        /// </summary>
        public void Insert(int index, int cardInstanceId)
        {
            if (cardInstanceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardInstanceId));
            }

            if (index < 0 || index > cardInstanceIds.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (IsFull)
            {
                throw new InvalidOperationException("The slot is full.");
            }

            if (cardInstanceIds.Contains(cardInstanceId))
            {
                throw new InvalidOperationException("The card already exists in the slot.");
            }

            cardInstanceIds.Insert(index, cardInstanceId);
        }

        /// <summary>
        /// 从卡槽移除卡牌实例ID。调用前必须保证该卡牌已存在于卡槽中。
        /// </summary>
        public void Remove(int cardInstanceId)
        {
            if (!cardInstanceIds.Remove(cardInstanceId))
            {
                throw new InvalidOperationException("The card does not exist in the slot.");
            }
        }

        /// <summary>
        /// 清空全部卡槽数据。仅允许在重置或重建游戏时调用。
        /// </summary>
        public void Clear()
        {
            cardInstanceIds.Clear();
        }
    }

    /// <summary>
    /// 单局麻将主玩法的完整纯数据模型。
    /// </summary>
    [Serializable]
    public sealed class MahjongGameModel
    {
        private readonly List<MahjongCardModel> cards; // 本局全部卡牌数据

        public IReadOnlyList<MahjongCardModel> Cards => cards; // 本局只读卡牌列表
        public MahjongLevelDefinition LevelDefinition { get; } // 本局使用的关卡配置
        public MahjongSlotModel Slot { get; } // 本局底部卡槽数据
        public MahjongGameState State { get; private set; } // 当前游戏状态

        /// <summary>
        /// 根据编辑器已校验的关卡和卡牌集合创建完整游戏数据。
        /// </summary>
        public MahjongGameModel(MahjongLevelDefinition levelDefinition, IEnumerable<MahjongCardModel> cards)
        {
            LevelDefinition = levelDefinition;
            this.cards = new List<MahjongCardModel>(cards);
            Slot = new MahjongSlotModel();
            State = MahjongGameState.Ready;
        }

        /// <summary>
        /// 根据实例ID查找卡牌。
        /// </summary>
        public MahjongCardModel GetCard(int instanceId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].InstanceId == instanceId)
                {
                    return cards[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 修改游戏状态。调用前必须完成胜负或生命周期相关校验。
        /// </summary>
        public void SetState(MahjongGameState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// 麻将主玩法的全局静态配置。
    /// </summary>
    public static class MahjongConfig
    {
        public const int SlotCapacity = 4; // 卡槽最大容量
        public const int MatchCount = 2; // 同类型卡牌消除所需数量
        public const int GridCoordinateStep = 1; // 标准整数网格中相邻格的逻辑坐标间隔
        public const int MaxLayerCount = 10; // 全局允许的最大层数，包含Layer 0
        public const int LevelCatalogVersion = 1; // 当前支持的关卡目录版本
        public const string LevelCatalogResourcePath = "Mahjong/Levels"; // Resources关卡目录加载路径
    }
}
