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
        private readonly int[] lastHintCardIds = new int[MahjongConfig.MatchCount]; // 上一次提示的卡牌实例ID

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
            Array.Clear(lastHintCardIds, 0, lastHintCardIds.Length);
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
            IReadOnlyList<int> eliminatedCardIds = slotRules.AddAndMarkMatches(cardInstanceId);
            IReadOnlyList<int> slotCardIdsBeforeElimination = new List<int>(Model.Slot.CardInstanceIds);
            if (eliminatedCardIds.Count == 0)
            {
                ResolveGameState();
            }

            return MahjongOperationResult.Success(
                Model.State,
                cardInstanceId,
                slotCardIdsBeforeElimination,
                eliminatedCardIds);
        }

        /// <summary>
        /// 在消除动画完成后移除已标记的卡槽卡牌并结算游戏状态。
        /// </summary>
        public MahjongGameState CompleteElimination(IReadOnlyList<int> cardInstanceIds)
        {
            for (int i = 0; i < cardInstanceIds.Count; i++)
            {
                int cardInstanceId = cardInstanceIds[i];
                Model.Slot.Remove(cardInstanceId);
                Model.GetCard(cardInstanceId).SetState(MahjongCardState.Eliminated);
            }

            ResolveGameState();
            return Model.State;
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
        /// 校验当前是否支持洗牌。仅游戏中的牌面卡牌可以参与洗牌。
        /// </summary>
        public MahjongOperationFailure ValidateShuffle()
        {
            if (Model == null || Model.State != MahjongGameState.Playing)
            {
                return MahjongOperationFailure.GameNotPlaying;
            }

            return MahjongOperationFailure.None;
        }

        /// <summary>
        /// 随机交换游戏区域内卡牌的完整棋盘位置，不影响卡槽内卡牌。
        /// </summary>
        public MahjongOperationResult Shuffle()
        {
            MahjongOperationFailure failure = ValidateShuffle();
            if (failure != MahjongOperationFailure.None)
            {
                MahjongGameState state = Model == null ? MahjongGameState.Ready : Model.State;
                return MahjongOperationResult.Failed(failure, state);
            }

            var boardCards = new List<MahjongCardModel>();
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                MahjongCardModel card = Model.Cards[i];
                if (card.State == MahjongCardState.OnBoard)
                {
                    boardCards.Add(card);
                }
            }

            var random = new Random();
            for (int i = boardCards.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                boardCards[i].SwapBoardPosition(boardCards[swapIndex]);
            }

            return MahjongOperationResult.Success(Model.State);
        }

        /// <summary>
        /// 校验当前是否支持撤销。仅允许撤回卡槽中最后一张未消除卡牌。
        /// </summary>
        public MahjongOperationFailure ValidateUndo()
        {
            if (Model == null || (Model.State != MahjongGameState.Playing && Model.State != MahjongGameState.Lost))
            {
                return MahjongOperationFailure.GameNotPlaying;
            }

            return Model.Slot.Count > 0
                ? MahjongOperationFailure.None
                : MahjongOperationFailure.CardNotFound;
        }

        /// <summary>
        /// 将卡槽中最后一张未消除卡牌撤回游戏区域。
        /// </summary>
        public MahjongOperationResult Undo()
        {
            MahjongOperationFailure failure = ValidateUndo();
            if (failure != MahjongOperationFailure.None)
            {
                MahjongGameState state = Model == null ? MahjongGameState.Ready : Model.State;
                return MahjongOperationResult.Failed(failure, state);
            }

            int cardInstanceId = Model.Slot.CardInstanceIds[Model.Slot.Count - 1];
            Model.Slot.Remove(cardInstanceId);
            Model.GetCard(cardInstanceId).SetState(MahjongCardState.OnBoard);
            Model.SetState(MahjongGameState.Playing);
            return MahjongOperationResult.Success(Model.State, cardInstanceId);
        }

        /// <summary>
        /// 查找一组可消除卡牌，优先选择牌面与卡槽的匹配组合并轮换提示候选。
        /// </summary>
        public IReadOnlyList<int> GetHintCardIds()
        {
            if (Model == null || Model.State != MahjongGameState.Playing)
            {
                return Array.Empty<int>();
            }

            var slotMatchCandidates = new List<int[]>();
            var boardMatchCandidates = new List<int[]>();
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                MahjongCardModel firstCard = Model.Cards[i];
                if (firstCard.State != MahjongCardState.OnBoard || ValidateSelectCard(firstCard.InstanceId) != MahjongOperationFailure.None)
                {
                    continue;
                }

                for (int j = 0; j < Model.Slot.CardInstanceIds.Count; j++)
                {
                    int slotCardId = Model.Slot.CardInstanceIds[j];
                    if (Model.GetCard(slotCardId).TypeId == firstCard.TypeId)
                    {
                        slotMatchCandidates.Add(new[] { firstCard.InstanceId, slotCardId });
                    }
                }

                for (int j = i + 1; j < Model.Cards.Count; j++)
                {
                    MahjongCardModel secondCard = Model.Cards[j];
                    if (secondCard.TypeId == firstCard.TypeId &&
                        secondCard.State == MahjongCardState.OnBoard &&
                        ValidateSelectCard(secondCard.InstanceId) == MahjongOperationFailure.None)
                    {
                        boardMatchCandidates.Add(new[] { firstCard.InstanceId, secondCard.InstanceId });
                    }
                }
            }

            slotMatchCandidates.AddRange(boardMatchCandidates);
            if (slotMatchCandidates.Count == 0)
            {
                return Array.Empty<int>();
            }

            int candidateIndex = GetNextHintCandidateIndex(slotMatchCandidates);
            int[] selectedCardIds = slotMatchCandidates[candidateIndex];
            lastHintCardIds[0] = selectedCardIds[0];
            lastHintCardIds[1] = selectedCardIds[1];
            return selectedCardIds;
        }

        /// <summary>
        /// 获取与上次提示不同的下一组候选索引，所有候选提示完成后从头轮换。
        /// </summary>
        private int GetNextHintCandidateIndex(IReadOnlyList<int[]> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                int[] candidate = candidates[i];
                if ((candidate[0] == lastHintCardIds[0] && candidate[1] == lastHintCardIds[1]) ||
                    (candidate[0] == lastHintCardIds[1] && candidate[1] == lastHintCardIds[0]))
                {
                    return (i + 1) % candidates.Count;
                }
            }

            return 0;
        }

        /// <summary>
        /// 根据剩余卡牌与卡槽状态结算当前游戏状态。调用前必须完成本次入槽和消除处理。
        /// </summary>
        private void ResolveGameState()
        {
            bool hasRemainingCard = false;
            bool hasPendingElimination = false;
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                MahjongCardState cardState = Model.Cards[i].State;
                if (cardState != MahjongCardState.Eliminated)
                {
                    hasRemainingCard = true;
                }

                if (cardState == MahjongCardState.PendingElimination)
                {
                    hasPendingElimination = true;
                }
            }

            if (!hasRemainingCard)
            {
                Model.SetState(MahjongGameState.Won);
            }
            else if (Model.Slot.IsFull && !hasPendingElimination)
            {
                Model.SetState(MahjongGameState.Lost);
            }
        }
    }
}
