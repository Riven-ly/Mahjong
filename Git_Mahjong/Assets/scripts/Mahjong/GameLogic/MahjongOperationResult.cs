using System;
using System.Collections.Generic;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将玩法操作失败原因。
    /// </summary>
    public enum MahjongOperationFailure
    {
        None,
        GameNotPlaying,
        CardNotFound,
        CardNotOnBoard,
        CardCovered,
        CardBlockedByBothSides,
        SlotFull,
        NoMatchingCardInSlot,
        FeatureNotImplemented
    }

    /// <summary>
    /// 麻将玩法单次业务操作结果。
    /// </summary>
    public sealed class MahjongOperationResult
    {
        private static readonly int[] EmptyCardIds = Array.Empty<int>(); // 无卡牌变化时复用的空集合

        public bool Succeeded { get; } // 本次操作是否成功
        public MahjongOperationFailure Failure { get; } // 操作失败原因
        public int MovedCardId { get; } // 本次进入卡槽的卡牌实例ID
        public IReadOnlyList<int> EliminatedCardIds { get; } // 本次消除的卡牌实例ID列表
        public MahjongGameState GameState { get; } // 操作完成后的游戏状态

        /// <summary>
        /// 创建完整操作结果。调用前必须保证成功状态、失败原因与结果数据彼此一致。
        /// </summary>
        private MahjongOperationResult(
            bool succeeded,
            MahjongOperationFailure failure,
            int movedCardId,
            IReadOnlyList<int> eliminatedCardIds,
            MahjongGameState gameState)
        {
            Succeeded = succeeded;
            Failure = failure;
            MovedCardId = movedCardId;
            EliminatedCardIds = eliminatedCardIds;
            GameState = gameState;
        }

        /// <summary>
        /// 创建成功结果。调用前必须保证失败原因为空，消除列表不包含无效实例ID。
        /// </summary>
        public static MahjongOperationResult Success(
            MahjongGameState gameState,
            int movedCardId = 0,
            IReadOnlyList<int> eliminatedCardIds = null)
        {
            return new MahjongOperationResult(
                true,
                MahjongOperationFailure.None,
                movedCardId,
                eliminatedCardIds ?? EmptyCardIds,
                gameState);
        }

        /// <summary>
        /// 创建失败结果。调用前必须提供非空失败原因，且本次操作不得修改业务数据。
        /// </summary>
        public static MahjongOperationResult Failed(
            MahjongOperationFailure failure,
            MahjongGameState gameState)
        {
            if (failure == MahjongOperationFailure.None)
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }

            return new MahjongOperationResult(false, failure, 0, EmptyCardIds, gameState);
        }
    }
}
