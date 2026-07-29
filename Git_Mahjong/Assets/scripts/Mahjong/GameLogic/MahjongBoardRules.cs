using System;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将牌面遮挡与可选状态规则服务。
    /// </summary>
    public sealed class MahjongBoardRules
    {
        private readonly MahjongGameModel model; // 当前参与判定的游戏数据

        /// <summary>
        /// 创建牌面规则服务。调用前必须提供有效游戏数据。
        /// </summary>
        public MahjongBoardRules(MahjongGameModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// 判断指定卡牌是否被更高层卡牌覆盖。调用前必须保证实例ID为正数。
        /// </summary>
        public bool IsCovered(int cardInstanceId)
        {
            MahjongCardModel card = model.GetCard(cardInstanceId);
            if (card == null || card.State != MahjongCardState.OnBoard)
            {
                return false;
            }

            for (int i = 0; i < model.Cards.Count; i++)
            {
                MahjongCardModel other = model.Cards[i];
                if (other.State != MahjongCardState.OnBoard || other.Layer <= card.Layer)
                {
                    continue;
                }

                if (MahjongLayoutGeometry.HasAreaOverlap(
                        card.Position.Column,
                        card.Position.Row,
                        card.Layer,
                        other.Position.Column,
                        other.Position.Row,
                        other.Layer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定卡牌同层同行的左右紧邻位置是否同时存在未移除卡牌。调用前必须保证实例ID为正数。
        /// </summary>
        public bool HasBothSideNeighbors(int cardInstanceId)
        {
            MahjongCardModel card = model.GetCard(cardInstanceId);
            if (card == null || card.State != MahjongCardState.OnBoard)
            {
                return false;
            }

            bool hasLeft = false;
            bool hasRight = false;
            int leftColumn = card.Position.Column - MahjongConfig.GridCoordinateStep;
            int rightColumn = card.Position.Column + MahjongConfig.GridCoordinateStep;

            for (int i = 0; i < model.Cards.Count; i++)
            {
                MahjongCardModel other = model.Cards[i];
                if (other.State != MahjongCardState.OnBoard ||
                    other.Layer != card.Layer ||
                    other.Position.Row != card.Position.Row)
                {
                    continue;
                }

                if (other.Position.Column == leftColumn)
                {
                    hasLeft = true;
                }
                else if (other.Position.Column == rightColumn)
                {
                    hasRight = true;
                }

                if (hasLeft && hasRight)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 校验指定卡牌能否进入卡槽。调用前必须保证游戏数据已完成初始化。
        /// </summary>
        public MahjongOperationFailure ValidateSelection(int cardInstanceId)
        {
            if (model.State != MahjongGameState.Playing)
            {
                return MahjongOperationFailure.GameNotPlaying;
            }

            MahjongCardModel card = model.GetCard(cardInstanceId);
            if (card == null)
            {
                return MahjongOperationFailure.CardNotFound;
            }

            if (card.State != MahjongCardState.OnBoard)
            {
                return MahjongOperationFailure.CardNotOnBoard;
            }

            if (IsCovered(cardInstanceId))
            {
                return MahjongOperationFailure.CardCovered;
            }

            if (HasBothSideNeighbors(cardInstanceId))
            {
                return MahjongOperationFailure.CardBlockedByBothSides;
            }

            if (model.Slot.IsFull)
            {
                return MahjongOperationFailure.SlotFull;
            }

            return MahjongOperationFailure.None;
        }
    }
}
