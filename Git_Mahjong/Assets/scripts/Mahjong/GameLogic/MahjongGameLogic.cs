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
            Array.Clear(lastHintCardIds, 0, lastHintCardIds.Length);
            Model.SetState(MahjongGameState.Playing);
            return MahjongOperationResult.Success(Model.State);
        }

        /// <summary>
        /// 校验牌面卡牌能否被选中。调用前必须先通过 StartNewGame 完成游戏初始化。
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
        /// 标记两张同类型牌等待表现层播放消除动画。
        /// </summary>
        public MahjongOperationResult MarkPairForElimination(int firstCardInstanceId, int secondCardInstanceId)
        {
            MahjongOperationFailure firstFailure = ValidateSelectCard(firstCardInstanceId);
            MahjongOperationFailure secondFailure = ValidateSelectCard(secondCardInstanceId);
            if (firstFailure != MahjongOperationFailure.None)
            {
                return MahjongOperationResult.Failed(firstFailure, Model == null ? MahjongGameState.Ready : Model.State);
            }

            if (secondFailure != MahjongOperationFailure.None)
            {
                return MahjongOperationResult.Failed(secondFailure, Model.State);
            }

            MahjongCardModel firstCard = Model.GetCard(firstCardInstanceId);
            MahjongCardModel secondCard = Model.GetCard(secondCardInstanceId);
            if (firstCard.TypeId != secondCard.TypeId)
            {
                return MahjongOperationResult.Failed(MahjongOperationFailure.NoMatchingCardInSlot, Model.State);
            }

            firstCard.SetState(MahjongCardState.PendingElimination);
            secondCard.SetState(MahjongCardState.PendingElimination);
            return MahjongOperationResult.Success(Model.State, eliminatedCardIds: new[] { firstCardInstanceId, secondCardInstanceId });
        }

        /// <summary>
        /// 在消除动画完成后更新两张卡牌状态并结算胜利。
        /// </summary>
        public MahjongGameState CompleteElimination(IReadOnlyList<int> cardInstanceIds)
        {
            for (int i = 0; i < cardInstanceIds.Count; i++)
            {
                Model.GetCard(cardInstanceIds[i]).SetState(MahjongCardState.Eliminated);
            }

            ResolveGameState();
            return Model.State;
        }

        /// <summary>
        /// 将本局状态置为失败，用于三次错误配对耗尽生命后的结算。
        /// </summary>
        public MahjongGameState LoseGame()
        {
            Model.SetState(MahjongGameState.Lost);
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
        /// 随机交换当前仍在牌面上的卡牌位置。
        /// </summary>
        public MahjongOperationResult Shuffle()
        {
            if (Model == null || Model.State != MahjongGameState.Playing)
            {
                return MahjongOperationResult.Failed(MahjongOperationFailure.GameNotPlaying, Model == null ? MahjongGameState.Ready : Model.State);
            }

            var boardCards = new List<MahjongCardModel>();
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                if (Model.Cards[i].State == MahjongCardState.OnBoard)
                {
                    boardCards.Add(Model.Cards[i]);
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
        /// 查找一组当前可操作的同类型牌并轮换提示候选。
        /// </summary>
        public IReadOnlyList<int> GetHintCardIds()
        {
            if (Model == null || Model.State != MahjongGameState.Playing)
            {
                return Array.Empty<int>();
            }

            var candidates = new List<int[]>();
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                MahjongCardModel firstCard = Model.Cards[i];
                if (firstCard.State != MahjongCardState.OnBoard || ValidateSelectCard(firstCard.InstanceId) != MahjongOperationFailure.None)
                {
                    continue;
                }

                for (int j = i + 1; j < Model.Cards.Count; j++)
                {
                    MahjongCardModel secondCard = Model.Cards[j];
                    if (secondCard.TypeId == firstCard.TypeId && secondCard.State == MahjongCardState.OnBoard &&
                        ValidateSelectCard(secondCard.InstanceId) == MahjongOperationFailure.None)
                    {
                        candidates.Add(new[] { firstCard.InstanceId, secondCard.InstanceId });
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return Array.Empty<int>();
            }

            int[] selectedCardIds = candidates[GetNextHintCandidateIndex(candidates)];
            lastHintCardIds[0] = selectedCardIds[0];
            lastHintCardIds[1] = selectedCardIds[1];
            return selectedCardIds;
        }

        /// <summary>
        /// 获取与上次提示不同的下一组候选索引。
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
        /// 根据剩余卡牌结算当前游戏状态。
        /// </summary>
        private void ResolveGameState()
        {
            for (int i = 0; i < Model.Cards.Count; i++)
            {
                if (Model.Cards[i].State != MahjongCardState.Eliminated)
                {
                    return;
                }
            }

            Model.SetState(MahjongGameState.Won);
        }
    }
}
