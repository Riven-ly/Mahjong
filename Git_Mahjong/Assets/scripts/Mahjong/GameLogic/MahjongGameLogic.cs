using System;
using System.Collections.Generic;
using MahjongGame.Model;

namespace MahjongGame.GameLogic
{
    /// <summary>
    /// 麻将主玩法业务逻辑统一入口。
    /// </summary>
    public sealed class MahjongGameLogic
    {
        private readonly MahjongLayoutGenerator layoutGenerator; // 卡牌布局生成器
        private MahjongBoardRules boardRules; // 当前牌面合法性规则
        private MahjongSlotRules slotRules; // 当前卡槽与消除规则

        public MahjongGameModel Model { get; private set; } // 当前只供外部读取的游戏数据

        /// <summary>
        /// 创建主玩法逻辑。调用后必须先执行 StartNewGame 才能操作卡牌。
        /// </summary>
        public MahjongGameLogic()
        {
            layoutGenerator = new MahjongLayoutGenerator();
        }

        /// <summary>
        /// 使用指定关卡配置开始新游戏。该方法会丢弃当前局数据，仅允许在确认重新开局后调用。
        /// </summary>
        public MahjongOperationResult StartNewGame(MahjongLevelDefinition levelDefinition)
        {
            List<MahjongCardModel> cards = layoutGenerator.Generate(levelDefinition);
            Model = new MahjongGameModel(levelDefinition, cards);
            boardRules = new MahjongBoardRules(Model);
            slotRules = new MahjongSlotRules(Model);
            Model.SetState(MahjongGameState.Playing);
            return MahjongOperationResult.Success(Model.State);
        }

        /// <summary>
        /// 校验卡牌选择操作。调用前必须先通过 StartNewGame 完成游戏初始化。
        /// </summary>
        public MahjongOperationFailure ValidateSelectCard(int cardInstanceId)
        {
            if (Model == null || boardRules == null)
            {
                return MahjongOperationFailure.GameNotPlaying;
            }

            if (cardInstanceId <= 0)
            {
                return MahjongOperationFailure.CardNotFound;
            }

            return boardRules.ValidateSelection(cardInstanceId);
        }

        /// <summary>
        /// 校验拖拽卡牌能否进入卡槽。除常规选择条件外，卡槽中必须已有同类型卡牌。
        /// </summary>
        public MahjongOperationFailure ValidateDragToSlot(int cardInstanceId)
        {
            MahjongOperationFailure failure = ValidateSelectCard(cardInstanceId);
            if (failure != MahjongOperationFailure.None)
            {
                return failure;
            }

            MahjongCardModel card = Model.GetCard(cardInstanceId);
            for (int i = 0; i < Model.Slot.CardInstanceIds.Count; i++)
            {
                MahjongCardModel slotCard = Model.GetCard(Model.Slot.CardInstanceIds[i]);
                if (slotCard.TypeId == card.TypeId)
                {
                    return MahjongOperationFailure.None;
                }
            }

            return MahjongOperationFailure.NoMatchingCardInSlot;
        }

        /// <summary>
        /// 选择卡牌并执行入槽、消除及胜负结算。调用前必须先开始游戏。
        /// </summary>
        public MahjongOperationResult SelectCard(int cardInstanceId)
        {
            MahjongOperationFailure failure = ValidateSelectCard(cardInstanceId);
            if (failure != MahjongOperationFailure.None)
            {
                MahjongGameState state = Model == null ? MahjongGameState.Ready : Model.State;
                return MahjongOperationResult.Failed(failure, state);
            }

            return ExecuteSelection(cardInstanceId);
        }

        /// <summary>
        /// 拖拽卡牌进入卡槽并执行消除与胜负结算。调用前必须先开始游戏，且卡槽中已有同类型卡牌。
        /// </summary>
        public MahjongOperationResult DragCardToSlot(int cardInstanceId)
        {
            MahjongOperationFailure failure = ValidateDragToSlot(cardInstanceId);
            if (failure != MahjongOperationFailure.None)
            {
                MahjongGameState state = Model == null ? MahjongGameState.Ready : Model.State;
                return MahjongOperationResult.Failed(failure, state);
            }

            return ExecuteSelection(cardInstanceId);
        }

        /// <summary>
        /// 执行已通过校验的卡牌入槽、消除与胜负结算。
        /// </summary>
        private MahjongOperationResult ExecuteSelection(int cardInstanceId)
        {
            IReadOnlyList<int> eliminatedCardIds = slotRules.AddAndResolveMatches(cardInstanceId);
            ResolveGameState();
            return MahjongOperationResult.Success(Model.State, cardInstanceId, eliminatedCardIds);
        }

        /// <summary>
        /// 判断指定卡牌是否被覆盖。调用前必须先开始游戏，且实例ID必须为正数。
        /// </summary>
        public bool IsCardCovered(int cardInstanceId)
        {
            if (boardRules == null)
            {
                throw new InvalidOperationException("游戏尚未开始。");
            }

            return boardRules.IsCovered(cardInstanceId);
        }

        /// <summary>
        /// 判断指定卡牌是否同时存在同层同行左右紧邻牌。调用前必须先开始游戏，且实例ID必须为正数。
        /// </summary>
        public bool HasBothSideNeighbors(int cardInstanceId)
        {
            if (boardRules == null)
            {
                throw new InvalidOperationException("游戏尚未开始。");
            }

            return boardRules.HasBothSideNeighbors(cardInstanceId);
        }

        /// <summary>
        /// 校验当前是否支持洗牌。当前阶段固定返回未实现，不修改任何游戏数据。
        /// </summary>
        public MahjongOperationFailure ValidateShuffle()
        {
            return MahjongOperationFailure.FeatureNotImplemented;
        }

        /// <summary>
        /// 请求洗牌。当前阶段固定返回失败，不修改任何游戏数据。
        /// </summary>
        public MahjongOperationResult Shuffle()
        {
            MahjongGameState state = Model == null ? MahjongGameState.Ready : Model.State;
            return MahjongOperationResult.Failed(ValidateShuffle(), state);
        }

        /// <summary>
        /// 校验当前是否支持撤销。当前阶段固定返回未实现，不修改任何游戏数据。
        /// </summary>
        public MahjongOperationFailure ValidateUndo()
        {
            return MahjongOperationFailure.FeatureNotImplemented;
        }

        /// <summary>
        /// 请求撤销。当前阶段固定返回失败，不修改任何游戏数据。
        /// </summary>
        public MahjongOperationResult Undo()
        {
            MahjongGameState state = Model == null ? MahjongGameState.Ready : Model.State;
            return MahjongOperationResult.Failed(ValidateUndo(), state);
        }

        /// <summary>
        /// 根据剩余卡牌与卡槽状态结算当前游戏状态。调用前必须完成本次入槽和消除处理。
        /// </summary>
        private void ResolveGameState()
        {
            bool hasRemainingCard = false;
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                if (Model.Cards[i].State != MahjongCardState.Eliminated)
                {
                    hasRemainingCard = true;
                    break;
                }
            }

            if (!hasRemainingCard)
            {
                Model.SetState(MahjongGameState.Won);
            }
            else if (Model.Slot.IsFull)
            {
                Model.SetState(MahjongGameState.Lost);
            }
        }
    }
}
