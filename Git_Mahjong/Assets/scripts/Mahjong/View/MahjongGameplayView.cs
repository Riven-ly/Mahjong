using System.Collections.Generic;
using DG.Tweening;
using MahjongGame.GameLogic;
using MahjongGame.Model;
using UnityEngine;

namespace MahjongGame.View
{
    /// <summary>
    /// 麻将主玩法的视图协调组件。
    /// </summary>
    public sealed class MahjongGameplayView : MonoBehaviour
    {
        [SerializeField] private RectTransform boardRoot; // 牌面卡牌根节点
        [SerializeField] private RectTransform adaptivePointTop; // 牌面允许区域的上边界锚点
        [SerializeField] private RectTransform adaptivePointBottom; // 牌面允许区域的下边界锚点
        private readonly Dictionary<int, MahjongCell> cellViews = new Dictionary<int, MahjongCell>(); // 实例ID到卡牌视图的映射
        private readonly Stack<MahjongCell> cellPool = new Stack<MahjongCell>(); // 可复用的麻将卡牌视图池
        private readonly Stack<ParticleSystem> eliminationEffectPool = new Stack<ParticleSystem>(); // 可复用的消除粒子特效池
        private readonly List<ParticleSystem> eliminationEffects = new List<ParticleSystem>(); // 已创建的消除粒子特效集合
        private RectTransform eliminationEffectRoot; // 消除粒子特效独立根节点
        private RectTransform eliminationLayer; // 消除卡牌置顶表现层
        private ParticleSystem eliminationEffectTemplate; // 消除粒子特效模板实例
        private MahjongGameLogic gameLogic; // 当前主玩法业务逻辑入口
        private int selectedCardId; // 当前已选中等待配对的卡牌实例ID
        private int remainingHealth; // 当前剩余生命数
        private int autoEliminationRemainingGroupCount; // 返回道具尚待自动消除的组数
        private int activeEliminationCount; // 当前正在播放消除动画的组数
        private int eliminatedCardCount; // 本局自上次随机奖励后累计消除的卡牌数量

        private const float MahjongCellHalfWidth = 85.5f; // 单张麻将牌局部坐标宽度的一半
        private const float MahjongCellHalfHeight = 99.5f; // 单张麻将牌局部坐标高度的一半
        private const float HorizontalScreenMargin = 50f; // 屏幕安全区左右保留边距

        public event System.Action<int> HealthChanged; // 剩余生命变化事件

        /// <summary>
        /// 初始化麻将主玩法业务逻辑。
        /// </summary>
        private void Awake()
        {
            gameLogic = new MahjongGameLogic();

            if(GameManager.Instance.beforeMahjongCells != null)
            {
                foreach (var cell in GameManager.Instance.beforeMahjongCells)
                {
                    RecycleCellView(cell);
                }
                GameManager.Instance.beforeMahjongCells = null;
            }

            eliminationLayer = transform.Find("EliminationLayer") as RectTransform;
            InitializeEliminationEffectPool();
        }

        /// <summary>
        /// 使用指定关卡配置重建本局玩法。调用前必须确认允许丢弃当前局进度。
        /// </summary>
        public void StartNewGame(MahjongLevelDefinition levelDefinition)
        {
            StopGameplay();
            ClearCellViews();
            eliminatedCardCount = 0;
            selectedCardId = 0;
            remainingHealth = MahjongViewConfig.InitialHealth;
            autoEliminationRemainingGroupCount = 0;
            ApplyBoardScale(levelDefinition);
            MahjongOperationResult result = gameLogic.StartNewGame(levelDefinition);
            BuildBoardViews();
            EnsurePlayablePair();
        }

        /// <summary>
        /// 根据上下自适应锚点和屏幕安全区左右边距限制全部卡牌完整边缘的牌面缩放。
        /// </summary>
        private void ApplyBoardScale(MahjongLevelDefinition levelDefinition)
        {
            float minimumCardX = float.MaxValue;
            float maximumCardX = float.MinValue;
            float minimumCardY = float.MaxValue;
            float maximumCardY = float.MinValue;
            for (int i = 0; i < levelDefinition.cards.Count; i++)
            {
                MahjongLevelCardDefinition card = levelDefinition.cards[i];
                float x = (card.coordY * 0.5f - (levelDefinition.gridColumnCount - 1) * 0.5f) * MahjongViewConfig.BoardCellWidth -
                          card.layer / 2 * MahjongViewConfig.LayerVisualOffsetX;
                float y = (card.coordX * 0.5f - (levelDefinition.gridRowCount - 1) * 0.5f) * MahjongViewConfig.BoardCellHeight;
                minimumCardX = Mathf.Min(minimumCardX, x - MahjongCellHalfWidth);
                maximumCardX = Mathf.Max(maximumCardX, x + MahjongCellHalfWidth);
                minimumCardY = Mathf.Min(minimumCardY, y - MahjongCellHalfHeight);
                maximumCardY = Mathf.Max(maximumCardY, y + MahjongCellHalfHeight);
            }

            RectTransform parent = boardRoot.parent as RectTransform;
            float topLimitY = parent.InverseTransformPoint(adaptivePointTop.position).y - boardRoot.anchoredPosition.y;
            float bottomLimitY = parent.InverseTransformPoint(adaptivePointBottom.position).y - boardRoot.anchoredPosition.y;
            Rect safeArea = Screen.safeArea;
            float leftSafeAreaRatio = safeArea.xMin / Screen.width;
            float rightSafeAreaRatio = safeArea.xMax / Screen.width;
            float localMargin = HorizontalScreenMargin * parent.rect.width / Screen.width;
            float leftLimitX = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, leftSafeAreaRatio) + localMargin - boardRoot.anchoredPosition.x;
            float rightLimitX = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, rightSafeAreaRatio) - localMargin - boardRoot.anchoredPosition.x;
            float topScale = maximumCardY > 0f ? topLimitY / maximumCardY : 1f;
            float bottomScale = minimumCardY < 0f ? bottomLimitY / minimumCardY : 1f;
            float leftScale = minimumCardX < 0f ? leftLimitX / minimumCardX : 1f;
            float rightScale = maximumCardX > 0f ? rightLimitX / maximumCardX : 1f;
            boardRoot.localScale = Vector3.one * Mathf.Min(1f, topScale, bottomScale, leftScale, rightScale);
        }

        /// <summary>
        /// 停止当前玩法的补间与交互。面板隐藏或重建游戏前调用。
        /// </summary>
        public void StopGameplay()
        {
            StopHint();
            DOTween.Kill(this);
            foreach (KeyValuePair<int, MahjongCell> pair in cellViews)
            {
                if (pair.Value != null)
                {
                    DOTween.Kill(pair.Value);
                }
            }

            foreach (MahjongCell cell in cellPool)
            {
                if (cell != null)
                {
                    DOTween.Kill(cell);
                }
            }

            SetBoardInput(false);
            ResetEliminationEffects();
            if (autoEliminationRemainingGroupCount > 0)
            {
                EndAutoElimination();
            }

            selectedCardId = 0;
            activeEliminationCount = 0;
        }

        /// <summary>
        /// 根据当前逻辑数据构建整個牌面视图。调用前必须已开始新游戏。
        /// </summary>
        private void BuildBoardViews()
        {
            int layerCount = gameLogic.Model.LevelDefinition.GetLayerCount();
            for (int layer = 0; layer < layerCount; layer++)
            {
                for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
                {
                    MahjongCardModel card = gameLogic.Model.Cards[i];
                    if (card.Layer != layer)
                    {
                        continue;
                    }

                    MahjongCell cell = GetCellView();
                    cell.Initialize(
                        card,
                        MahjongCardVisualCatalogLoader.GetSprite(card.TypeId),
                        MahjongCardColorUtility.GetColor(card.TypeId),
                        HandleCellSelectRequested);
                    cell.SetBoardPosition(GetBoardPosition(card, gameLogic.Model.LevelDefinition));
                    cell.transform.SetAsLastSibling();
                    cellViews.Add(card.InstanceId, cell);
                }
            }
        }

        /// <summary>
        /// 处理卡牌视图的点击选择、取消选择或配对消除。
        /// </summary>
        private void HandleCellSelectRequested(MahjongCell cell)
        {
            if (activeEliminationCount != 0)
            {
                return;
            }

            cell.SetHintEffectActive(false);
            MahjongOperationFailure failure = gameLogic.ValidateSelectCard(cell.InstanceId);
            if (failure != MahjongOperationFailure.None)
            {
                cell.AnimateRejected();
                return;
            }

            if (selectedCardId == 0)
            {
                selectedCardId = cell.InstanceId;
                cell.SetSelectionEffectActive(true);
                return;
            }

            if (selectedCardId == cell.InstanceId)
            {
                selectedCardId = 0;
                cell.SetSelectionEffectActive(false);
                return;
            }

            MahjongCell firstCell = cellViews[selectedCardId];
            MahjongOperationResult result = gameLogic.MarkPairForElimination(selectedCardId, cell.InstanceId);
            selectedCardId = 0;
            if (!result.Succeeded)
            {
                firstCell.SetSelectionEffectActive(true);
                cell.SetSelectionEffectActive(true);
                SetBoardInput(false);
                Sequence rejectSequence = DOTween.Sequence();
                rejectSequence.Join(firstCell.AnimateRejected());
                rejectSequence.Join(cell.AnimateRejected());
                rejectSequence.AppendCallback(() =>
                {
                    firstCell.SetSelectionEffectActive(false);
                    cell.SetSelectionEffectActive(false);
                    StopHint();
                    RefreshBoardStates();
                });
                if (result.Failure == MahjongOperationFailure.NoMatchingCardInSlot)
                {
                    remainingHealth--;
                    HealthChanged?.Invoke(remainingHealth);
                    if (remainingHealth <= 0)
                    {
                        DOVirtual.DelayedCall(MahjongViewConfig.HealthFadeDuration, () =>
                            TriggerGameResultEvent(gameLogic.LoseGame()))
                            .SetTarget(this);
                    }
                }

                return;
            }

            StopHint();
            firstCell.SetSelectionEffectActive(true);
            cell.SetSelectionEffectActive(true);
            PlayEliminationAnimation(result);
        }

        /// <summary>
        /// 随机交换游戏区域卡牌的完整棋盘位置并刷新牌面显示。
        /// </summary>
        public bool TryShuffle()
        {
            if (gameLogic == null ||
                gameLogic.Model == null ||
                gameLogic.Model.State != MahjongGameState.Playing ||
                activeEliminationCount != 0)
            {
                return false;
            }

            if (selectedCardId != 0)
            {
                cellViews[selectedCardId].SetSelectionEffectActive(false);
                selectedCardId = 0;
            }

            MahjongOperationResult result = gameLogic.Shuffle();
            if (!result.Succeeded)
            {
                return false;
            }

            RefreshBoardPositions();
            EnsurePlayablePair();
            return true;
        }

        /// <summary>
        /// 确保当前可操作牌中至少存在一组同类型配对；不存在时保留牌面并洗牌后重试。
        /// </summary>
        private void EnsurePlayablePair()
        {
            if (gameLogic.Model.State != MahjongGameState.Playing)
            {
                return;
            }

            while (gameLogic.GetHintCardIds().Count == 0)
            {
                MahjongOperationResult shuffleResult = gameLogic.Shuffle();
                if (!shuffleResult.Succeeded)
                {
                    break;
                }
            }

            RefreshBoardPositions();
            RefreshBoardStates();
        }

        /// <summary>
        /// 使用返回道具自动消除指定数量的可操作同类型牌组。
        /// </summary>
        public bool TryAutoEliminate(int groupCount)
        {
            if (gameLogic == null ||
                gameLogic.Model == null ||
                gameLogic.Model.State != MahjongGameState.Playing ||
                activeEliminationCount != 0 ||
                groupCount <= 0)
            {
                return false;
            }

            autoEliminationRemainingGroupCount = groupCount;
            StopHint();
            UIManager.Instance.OpenUIMask();
            if (selectedCardId != 0)
            {
                IReadOnlyList<int> selectedPairCardIds = gameLogic.GetHintCardIdsForCard(selectedCardId);
                cellViews[selectedCardId].SetSelectionEffectActive(false);
                selectedCardId = 0;
                if (selectedPairCardIds.Count != 0)
                {
                    MahjongOperationResult result = gameLogic.MarkPairForElimination(selectedPairCardIds[0], selectedPairCardIds[1]);
                    if (result.Succeeded)
                    {
                        PlayEliminationAnimation(result);
                        return true;
                    }
                }
            }

            StartNextAutoElimination();
            return true;
        }

        /// <summary>
        /// 查找下一组可操作配对；找不到时保留剩余牌并洗牌后继续尝试。
        /// </summary>
        private void StartNextAutoElimination()
        {
            if (autoEliminationRemainingGroupCount <= 0 || gameLogic.Model.State != MahjongGameState.Playing)
            {
                EndAutoElimination();
                return;
            }

            IReadOnlyList<int> cardIds = gameLogic.GetHintCardIds();
            if (cardIds.Count == 0)
            {
                MahjongOperationResult shuffleResult = gameLogic.Shuffle();
                if (!shuffleResult.Succeeded)
                {
                    autoEliminationRemainingGroupCount = 0;
                    RefreshBoardStates();
                    EndAutoElimination();
                    return;
                }

                RefreshBoardPositions();
                RefreshBoardStates();
                DOVirtual.DelayedCall(0f, StartNextAutoElimination).SetTarget(this);
                return;
            }

            MahjongOperationResult result = gameLogic.MarkPairForElimination(cardIds[0], cardIds[1]);
            if (!result.Succeeded)
            {
                autoEliminationRemainingGroupCount = 0;
                RefreshBoardStates();
                EndAutoElimination();
                return;
            }

            PlayEliminationAnimation(result);
        }

        /// <summary>
        /// 结束返回道具的自动消除并解除全局输入遮罩。
        /// </summary>
        private void EndAutoElimination()
        {
            autoEliminationRemainingGroupCount = 0;
            UIManager.Instance.HideUIMask();
        }

        /// <summary>
        /// 找到一组可消除的牌面卡牌并显示提示特效。
        /// </summary>
        public bool TryShowHint()
        {
            if (gameLogic == null ||
                gameLogic.Model == null ||
                gameLogic.Model.State != MahjongGameState.Playing ||
                activeEliminationCount != 0)
            {
                return false;
            }

            IReadOnlyList<int> hintCardIds;
            if (selectedCardId != 0)
            {
                hintCardIds = gameLogic.GetHintCardIdsForCard(selectedCardId);
                if (hintCardIds.Count == 0)
                {
                    cellViews[selectedCardId].SetSelectionEffectActive(false);
                    selectedCardId = 0;
                    hintCardIds = gameLogic.GetHintCardIds();
                }
            }
            else
            {
                hintCardIds = gameLogic.GetHintCardIds();
            }

            if (hintCardIds.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < hintCardIds.Count; i++)
            {
                cellViews[hintCardIds[i]].SetHintEffectActive(true);
            }

            return true;
        }

        /// <summary>
        /// 关闭全部卡牌的提示特效。
        /// </summary>
        public void StopHint()
        {
            foreach (KeyValuePair<int, MahjongCell> pair in cellViews)
            {
                pair.Value.SetHintEffectActive(false);
            }
        }

        /// <summary>
        /// 判断当前是否没有入槽或消除动画，允许执行道具操作。
        /// </summary>
        private bool IsStable()
        {
            return gameLogic != null &&
                   gameLogic.Model != null &&
                   gameLogic.Model.State == MahjongGameState.Playing &&
                   selectedCardId == 0 &&
                   activeEliminationCount == 0;
        }

        /// <summary>
        /// 根据当前逻辑中的层级与坐标更新全部牌面卡牌位置和显示层级。
        /// </summary>
        private void RefreshBoardPositions()
        {
            int layerCount = gameLogic.Model.LevelDefinition.GetLayerCount();
            for (int layer = 0; layer < layerCount; layer++)
            {
                for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
                {
                    MahjongCardModel card = gameLogic.Model.Cards[i];
                    if (card.State != MahjongCardState.OnBoard || card.Layer != layer ||
                        !cellViews.TryGetValue(card.InstanceId, out MahjongCell cell))
                    {
                        continue;
                    }

                    cell.SetBoardPosition(GetBoardPosition(card, gameLogic.Model.LevelDefinition));
                    cell.RefreshVisual(
                        MahjongCardVisualCatalogLoader.GetSprite(card.TypeId),
                        MahjongCardColorUtility.GetColor(card.TypeId));
                    cell.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// 播放一组已到达卡槽的卡牌消除动画。
        /// </summary>
        private void PlayEliminationAnimation(MahjongOperationResult result)
        {
            activeEliminationCount++;
            SetBoardInput(false);
            MahjongCell firstCell = cellViews[result.EliminatedCardIds[0]];
            MahjongCell secondCell = cellViews[result.EliminatedCardIds[1]];
            Vector3 collisionWorldPosition = (firstCell.RectTransform.position + secondCell.RectTransform.position) * 0.5f;
            firstCell.transform.SetParent(eliminationLayer, true);
            secondCell.transform.SetParent(eliminationLayer, true);
            firstCell.transform.SetAsLastSibling();
            secondCell.transform.SetAsLastSibling();
            Vector2 centerPosition = eliminationLayer.InverseTransformPoint(collisionWorldPosition);
            Vector2 firstTransitPosition = centerPosition + Vector2.left * MahjongViewConfig.TransitOffsetX;
            Vector2 secondTransitPosition = centerPosition + Vector2.right * MahjongViewConfig.TransitOffsetX;
            if (firstCell.RectTransform.position.x > secondCell.RectTransform.position.x)
            {
                Vector2 swapPosition = firstTransitPosition;
                firstTransitPosition = secondTransitPosition;
                secondTransitPosition = swapPosition;
            }

            Vector2 firstCollisionPosition = centerPosition + Vector2.left * MahjongViewConfig.CollisionHalfDistance;
            Vector2 secondCollisionPosition = centerPosition + Vector2.right * MahjongViewConfig.CollisionHalfDistance;
            Vector2 firstReboundPosition = firstCollisionPosition + Vector2.left * MahjongViewConfig.ReboundOffsetX;
            Vector2 secondReboundPosition = secondCollisionPosition + Vector2.right * MahjongViewConfig.ReboundOffsetX;
            if (firstCell.RectTransform.position.x > secondCell.RectTransform.position.x)
            {
                Vector2 swapPosition = firstCollisionPosition;
                firstCollisionPosition = secondCollisionPosition;
                secondCollisionPosition = swapPosition;
                swapPosition = firstReboundPosition;
                firstReboundPosition = secondReboundPosition;
                secondReboundPosition = swapPosition;
            }

            Sequence eliminateSequence = DOTween.Sequence();
            eliminateSequence.Join(firstCell.AnimateToEliminationPoint(
                eliminationLayer,
                firstTransitPosition,
                firstCollisionPosition,
                firstReboundPosition,
                firstCollisionPosition));
            eliminateSequence.Join(secondCell.AnimateToEliminationPoint(
                eliminationLayer,
                secondTransitPosition,
                secondCollisionPosition,
                secondReboundPosition,
                secondCollisionPosition));
            eliminateSequence.AppendCallback(() =>
            {
                PlayEliminationEffect(collisionWorldPosition);
                firstCell.AnimateEliminated(null);
                secondCell.AnimateEliminated(null);
            });
            eliminateSequence.AppendInterval(MahjongViewConfig.EliminateDuration);
            eliminateSequence.SetTarget(this).AppendCallback(() => CompleteEliminationAnimation(result));
        }


        /// <summary>
        /// 回收已完成消除动画的卡牌视图，并刷新卡槽布局和牌面状态。
        /// </summary>
        private void CompleteEliminationAnimation(MahjongOperationResult result)
        {
            MahjongGameState gameState = gameLogic.CompleteElimination(result.EliminatedCardIds);
            for (int i = 0; i < result.EliminatedCardIds.Count; i++)
            {
                int eliminatedCardId = result.EliminatedCardIds[i];
                if (cellViews.TryGetValue(eliminatedCardId, out MahjongCell eliminatedCell))
                {
                    cellViews.Remove(eliminatedCardId);
                    RecycleCellView(eliminatedCell);
                }
            }

            activeEliminationCount--;
            if (gameState == MahjongGameState.Playing)
            {
                TryShowEliminationReward(result.EliminatedCardIds.Count, () =>
                {
                    if (autoEliminationRemainingGroupCount > 0)
                    {
                        autoEliminationRemainingGroupCount--;
                        EnsurePlayablePair();
                        if (autoEliminationRemainingGroupCount > 0)
                        {
                            DOVirtual.DelayedCall(0f, StartNextAutoElimination).SetTarget(this);
                        }
                        else
                        {
                            EndAutoElimination();
                        }
                    }
                    else
                    {
                        EnsurePlayablePair();
                    }
                });
            }
            else if (autoEliminationRemainingGroupCount > 0)
            {
                EndAutoElimination();
            }

            TriggerGameResultEvent(gameState);
        }

        /// <summary>
        /// 在消除动画结束后累计消除卡牌，并按配置概率弹出随机奖励。
        /// </summary>
        private void TryShowEliminationReward(int eliminatedCount,System.Action _callback)
        {
            eliminatedCardCount += eliminatedCount;
            if (eliminatedCardCount < MahjongConfig.RewardTriggerEliminatedCardCount)
            {
                _callback?.Invoke();
                return;
            }

            int extraGroupCount = (eliminatedCardCount - MahjongConfig.RewardTriggerEliminatedCardCount) /
                                  MahjongConfig.MatchCount;
            float probability = Mathf.Min(
                1f,
                MahjongConfig.RewardInitialProbability +
                extraGroupCount * MahjongConfig.RewardProbabilityIncreasePerGroup);
            if (Random.value >= probability)
            {
                _callback?.Invoke();
                return;
            }

            eliminatedCardCount = 0;
            bool isAutoEliminating = autoEliminationRemainingGroupCount > 0;
            if (isAutoEliminating)
            {
                UIManager.Instance.HideUIMask();
            }

            UIManager.Instance.OpenUI<GeneralRewardsPanel>(null, () =>
            {
                if (isAutoEliminating && autoEliminationRemainingGroupCount > 0)
                {
                    UIManager.Instance.OpenUIMask();
                }

                _callback?.Invoke();
            });
        }

        /// <summary>
        /// 初始化独立消除粒子特效池。调用前必须已配置特效根节点及至少一个粒子实例。
        /// </summary>
        private void InitializeEliminationEffectPool()
        {
            eliminationEffectRoot = transform.Find("EliminationEffectRoot") as RectTransform;
            if (eliminationEffectRoot == null)
            {
                throw new MissingReferenceException("未找到消除粒子特效根节点。");
            }

            for (int i = 0; i < eliminationEffectRoot.childCount; i++)
            {
                ParticleSystem effect = eliminationEffectRoot.GetChild(i).GetComponent<ParticleSystem>();
                if (effect == null)
                {
                    continue;
                }

                if (eliminationEffectTemplate == null)
                {
                    eliminationEffectTemplate = effect;
                }

                eliminationEffects.Add(effect);
                RecycleEliminationEffect(effect);
            }

            if (eliminationEffectTemplate == null)
            {
                throw new MissingReferenceException("消除粒子特效根节点下未配置粒子特效。");
            }
        }

        /// <summary>
        /// 在指定世界坐标播放一次消除粒子特效。
        /// </summary>
        private void PlayEliminationEffect(Vector3 worldPosition)
        {
            ParticleSystem effect = eliminationEffectPool.Count > 0
                ? eliminationEffectPool.Pop()
                : CreateEliminationEffect();
            Transform effectTransform = effect.transform;
            effectTransform.position = worldPosition;
            effectTransform.SetAsLastSibling();
            effect.gameObject.SetActive(true);
            effect.Play(true);
            DOVirtual.DelayedCall(MahjongViewConfig.EliminationEffectDuration, () => RecycleEliminationEffect(effect))
                .SetTarget(effect);
        }

        /// <summary>
        /// 创建一份消除粒子特效并纳入对象池管理。
        /// </summary>
        private ParticleSystem CreateEliminationEffect()
        {
            ParticleSystem effect = Instantiate(eliminationEffectTemplate, eliminationEffectRoot);
            eliminationEffects.Add(effect);
            return effect;
        }

        /// <summary>
        /// 停止并回收指定消除粒子特效。
        /// </summary>
        private void RecycleEliminationEffect(ParticleSystem effect)
        {
            DOTween.Kill(effect);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.gameObject.SetActive(false);
            eliminationEffectPool.Push(effect);
        }

        /// <summary>
        /// 停止全部消除粒子特效并恢复对象池可用状态。
        /// </summary>
        private void ResetEliminationEffects()
        {
            eliminationEffectPool.Clear();
            for (int i = 0; i < eliminationEffects.Count; i++)
            {
                RecycleEliminationEffect(eliminationEffects[i]);
            }
        }

        /// <summary>
        /// 根据游戏结果触发全局胜利或失败事件。
        /// </summary>
        private static void TriggerGameResultEvent(MahjongGameState gameState)
        {
            if (gameState == MahjongGameState.Won)
            {
                EventManager.Instance.TriggerEvent(GameEvent.MahjongGameWon);
            }
            else if (gameState == MahjongGameState.Lost)
            {
                EventManager.Instance.TriggerEvent(GameEvent.MahjongGameLost);
            }
        }

        /// <summary>
        /// 根据业务逻辑刷新牌面卡牌的遮挡、邻牌阻挡与交互表现。
        /// </summary>
        private void RefreshBoardStates()
        {
            if (gameLogic.Model.State == MahjongGameState.Lost)
            {
                return;
            }

            for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
            {
                MahjongCardModel card = gameLogic.Model.Cards[i];
                if (card.State != MahjongCardState.OnBoard || !cellViews.TryGetValue(card.InstanceId, out MahjongCell cell))
                {
                    continue;
                }

                bool blocked = gameLogic.Model.State != MahjongGameState.Playing ||
                               gameLogic.IsCardCovered(card.InstanceId);
                cell.SetBlocked(blocked);
            }
        }

        /// <summary>
        /// 统一设置所有牌面卡牌是否允许交互。
        /// </summary>
        private void SetBoardInput(bool interactable)
        {
            if (gameLogic == null || gameLogic.Model == null)
            {
                return;
            }

            for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
            {
                MahjongCardModel card = gameLogic.Model.Cards[i];
                if (card.State == MahjongCardState.OnBoard && cellViews.TryGetValue(card.InstanceId, out MahjongCell cell))
                {
                    cell.SetInteractable(interactable);
                }
            }
        }

        /// <summary>
        /// 从已预创建的对象池获取卡牌视图。
        /// </summary>
        private MahjongCell GetCellView()
        {
            
            MahjongCell cell = cellPool.Count > 0
                ? cellPool.Pop()
                : Instantiate(GameManager.Instance.mahjongCellPrefab, boardRoot);
            cell.transform.SetParent(boardRoot, false);
            cell.gameObject.SetActive(true);
            return cell;
        }

        /// <summary>
        /// 重置并回收卡牌视图。空引用会被忽略。
        /// </summary>
        private void RecycleCellView(MahjongCell cell)
        {
            if (cell == null)
            {
                return;
            }

            cell.ResetForPool(boardRoot);
            cellPool.Push(cell);
        }

        /// <summary>
        /// 将当前全部活动卡牌视图回收到对象池并清空实例映射。
        /// </summary>
        private void ClearCellViews()
        {
            foreach (KeyValuePair<int, MahjongCell> pair in cellViews)
            {
                RecycleCellView(pair.Value);
            }

            cellViews.Clear();
        }

        /// <summary>
        /// 将纯逻辑网格坐标按层级奇偶的半格错位规则换算为牌面 UGUI 局部坐标。
        /// </summary>
        private static Vector2 GetBoardPosition(MahjongCardModel card, MahjongLevelDefinition levelDefinition)
        {
            float centerColumn = card.CoordY * 0.5f;
            float centerRow = card.CoordX * 0.5f;
            float maxColumn = levelDefinition.gridColumnCount - 1;
            float maxRow = levelDefinition.gridRowCount - 1;
            float layerVisualOffset = card.Layer / 2 * MahjongViewConfig.LayerVisualOffsetX;
            float x = (centerColumn - maxColumn * 0.5f) * MahjongViewConfig.BoardCellWidth - layerVisualOffset;
            float y = (centerRow - maxRow * 0.5f) * MahjongViewConfig.BoardCellHeight;
            return new Vector2(x, y);
        }


    }
}
