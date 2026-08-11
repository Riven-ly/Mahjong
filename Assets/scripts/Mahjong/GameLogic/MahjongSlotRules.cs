using System;
using System.Collections.Generic;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将卡槽入槽与配对消除规则服务。
    /// </summary>
    public sealed class MahjongSlotRules
    {
        private readonly MahjongGameModel model; // 当前参与卡槽运算的游戏数据

        /// <summary>
        /// 创建卡槽规则服务。调用前必须提供有效游戏数据。
        /// </summary>
        public MahjongSlotRules(MahjongGameModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// 校验卡牌能否加入卡槽。调用前必须保证卡牌实例ID为正数。
        /// </summary>
        public MahjongOperationFailure ValidateAdd(int cardInstanceId)
        {
            MahjongCardModel card = model.GetCard(cardInstanceId);
            if (card == null)
            {
                return MahjongOperationFailure.CardNotFound;
            }

            if (card.State != MahjongCardState.OnBoard)
            {
                return MahjongOperationFailure.CardNotOnBoard;
            }

            return model.Slot.IsFull
                ? MahjongOperationFailure.SlotFull
                : MahjongOperationFailure.None;
        }

        /// <summary>
        /// 将卡牌加入卡槽并标记本次匹配卡牌。匹配卡牌仍保留在卡槽中占位，等待动画完成后再移除。
        /// </summary>
        public IReadOnlyList<int> AddAndMarkMatches(int cardInstanceId)
        {
            MahjongOperationFailure failure = ValidateAdd(cardInstanceId);
            if (failure != MahjongOperationFailure.None)
            {
                throw new InvalidOperationException($"卡牌入槽校验失败：{failure}");
            }

            MahjongCardModel card = model.GetCard(cardInstanceId);
            int insertIndex = GetInsertIndex(card.TypeId);
            card.SetState(MahjongCardState.InSlot);
            model.Slot.Insert(insertIndex, cardInstanceId);

            var matchedCardIds = new List<int>(MahjongConfig.MatchCount);
            for (int i = 0; i < model.Slot.CardInstanceIds.Count; i++)
            {
                int slotCardId = model.Slot.CardInstanceIds[i];
                MahjongCardModel slotCard = model.GetCard(slotCardId);
                if (slotCard.TypeId == card.TypeId && slotCard.State == MahjongCardState.InSlot)
                {
                    matchedCardIds.Add(slotCardId);
                    if (matchedCardIds.Count == MahjongConfig.MatchCount)
                    {
                        break;
                    }
                }
            }

            if (matchedCardIds.Count < MahjongConfig.MatchCount)
            {
                matchedCardIds.Clear();
                return matchedCardIds;
            }

            for (int i = 0; i < matchedCardIds.Count; i++)
            {
                model.GetCard(matchedCardIds[i]).SetState(MahjongCardState.PendingElimination);
            }

            return matchedCardIds;
        }

        /// <summary>
        /// 获取指定类型卡牌的入槽索引；存在同类型卡牌时紧随最后一张，否则追加到末尾。
        /// </summary>
        private int GetInsertIndex(int typeId)
        {
            for (int i = model.Slot.Count - 1; i >= 0; i--)
            {
                MahjongCardModel slotCard = model.GetCard(model.Slot.CardInstanceIds[i]);
                if (slotCard.TypeId == typeId && slotCard.State == MahjongCardState.InSlot)
                {
                    return i + 1;
                }
            }

            return model.Slot.Count;
        }
    }
}
