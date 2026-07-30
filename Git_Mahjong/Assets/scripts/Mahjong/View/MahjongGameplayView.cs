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
        [SerializeField] private RectTransform slotRoot; // 底部卡槽根节点
        [SerializeField] private RectTransform dragLayer; // 拖拽期间临时置顶层

        private readonly Dictionary<int, MahjongCell> cellViews = new Dictionary<int, MahjongCell>(); // 实例ID到卡牌视图的映射
        private readonly Stack<MahjongCell> cellPool = new Stack<MahjongCell>(); // 可复用的麻将卡牌视图池
        private readonly Stack<ParticleSystem> eliminationEffectPool = new Stack<ParticleSystem>(); // 可复用的消除粒子特效池
        private readonly List<ParticleSystem> eliminationEffects = new List<ParticleSystem>(); // 已创建的消除粒子特效集合
        private RectTransform eliminationEffectRoot; // 消除粒子特效独立根节点
        private ParticleSystem eliminationEffectTemplate; // 消除粒子特效模板实例
        private MahjongGameLogic gameLogic; // 当前主玩法业务逻辑入口
        private readonly HashSet<int> movingCardIds = new HashSet<int>(); // 当前正在进入卡槽的卡牌实例ID集合
        private readonly Dictionary<int, Tween> slotMoveTweens = new Dictionary<int, Tween>(); // 当前卡牌实例ID对应的入槽补间
        private readonly List<MahjongOperationResult> pendingEliminationResults = new List<MahjongOperationResult>(); // 等待相关卡牌到达卡槽的消除结果集合
        private int activeEliminationCount; // 当前正在播放消除动画的组数

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

            InitializeEliminationEffectPool();
        }

        /// <summary>
        /// 使用指定关卡配置重建本局玩法。调用前必须确认允许丢弃当前局进度。
        /// </summary>
        public void StartNewGame(MahjongLevelDefinition levelDefinition)
        {
            StopGameplay();
            ClearCellViews();
            MahjongOperationResult result = gameLogic.StartNewGame(levelDefinition);
            BuildBoardViews();
            RefreshBoardStates();
        }

        /// <summary>
        /// 停止当前玩法的补间与交互。面板隐藏或重建游戏前调用。
        /// </summary>
        public void StopGameplay()
        {
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
            movingCardIds.Clear();
            slotMoveTweens.Clear();
            pendingEliminationResults.Clear();
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
                        dragLayer,
                        slotRoot,
                        MahjongCardVisualCatalogLoader.GetSprite(card.TypeId),
                        MahjongCardColorUtility.GetColor(card.TypeId),
                        HandleCellSelectRequested);
                    cell.SetBoardPosition(GetBoardPosition(card.Position, card.Layer, gameLogic.Model.LevelDefinition));
                    cell.transform.SetAsLastSibling();
                    cellViews.Add(card.InstanceId, cell);
                }
            }
        }

        /// <summary>
        /// 处理卡牌视图的选择请求，并立即播放本次入槽动画。
        /// </summary>
        private void HandleCellSelectRequested(MahjongCell cell, bool isDraggedToSlot)
        {
            int slotInsertIndex = GetSlotInsertIndex(cell.InstanceId);
            MahjongOperationResult result = isDraggedToSlot
                ? gameLogic.DragCardToSlot(cell.InstanceId)
                : gameLogic.SelectCard(cell.InstanceId);
            if (!result.Succeeded)
            {
                Tween failureTween = isDraggedToSlot && result.Failure == MahjongOperationFailure.NoMatchingCardInSlot
                    ? cell.AnimateBack()
                    : cell.AnimateRejected();
                failureTween.OnComplete(RefreshBoardStates);
                return;
            }

            cell.SetInteractable(false);
            if (result.GameState == MahjongGameState.Playing)
            {
                RefreshBoardStates();
            }
            else
            {
                SetBoardInput(false);
            }

            movingCardIds.Add(cell.InstanceId);
            AnimateSlotCellsAfterInsertion(result.SlotCardIdsBeforeElimination, slotInsertIndex);
            Vector2 slotPosition = GetSlotPosition(slotInsertIndex);
            Tween moveTween = isDraggedToSlot
                ? cell.AnimateDraggedTo(slotRoot, slotPosition)
                : cell.AnimateTo(slotRoot, slotPosition);
            StartSlotMove(cell.InstanceId, moveTween);
            if (result.EliminatedCardIds.Count > 0)
            {
                pendingEliminationResults.Add(result);
            }
        }

        /// <summary>
        /// 开始或替换指定卡牌的入槽补间。替换时仅终止该卡牌的旧补间。
        /// </summary>
        private void StartSlotMove(int cardInstanceId, Tween moveTween)
        {
            if (slotMoveTweens.TryGetValue(cardInstanceId, out Tween previousTween))
            {
                previousTween.Kill();
            }

            slotMoveTweens[cardInstanceId] = moveTween;
            moveTween.OnComplete(() => CompleteMoveToSlot(cardInstanceId, moveTween));
        }

        /// <summary>
        /// 记录卡牌到达卡槽，并检查是否可以开始配对消除动画。
        /// </summary>
        private void CompleteMoveToSlot(int cardInstanceId, Tween completedTween)
        {
            if (!slotMoveTweens.TryGetValue(cardInstanceId, out Tween activeTween) || activeTween != completedTween)
            {
                return;
            }

            slotMoveTweens.Remove(cardInstanceId);
            movingCardIds.Remove(cardInstanceId);
            TryPlayPendingEliminations();
            TryUpdateSlotLayout();
        }

        /// <summary>
        /// 在同组待消除卡牌都到达卡槽后，播放消除动画并回收视图。
        /// </summary>
        private void TryPlayPendingEliminations()
        {
            for (int i = pendingEliminationResults.Count - 1; i >= 0; i--)
            {
                MahjongOperationResult result = pendingEliminationResults[i];
                bool allEliminatedCardsArrived = true;
                for (int j = 0; j < result.EliminatedCardIds.Count; j++)
                {
                    if (movingCardIds.Contains(result.EliminatedCardIds[j]))
                    {
                        allEliminatedCardsArrived = false;
                        break;
                    }
                }

                if (!allEliminatedCardsArrived)
                {
                    continue;
                }

                pendingEliminationResults.RemoveAt(i);
                PlayEliminationAnimation(result);
            }
        }

        /// <summary>
        /// 播放一组已到达卡槽的卡牌消除动画。
        /// </summary>
        private void PlayEliminationAnimation(MahjongOperationResult result)
        {
            activeEliminationCount++;
            Sequence eliminateSequence = DOTween.Sequence();
            for (int i = result.EliminatedCardIds.Count - 1; i >= 0; i--)
            {
                if (cellViews.TryGetValue(result.EliminatedCardIds[i], out MahjongCell eliminatedCell))
                {
                    eliminateSequence.Join(eliminatedCell.AnimateEliminated(() =>
                        PlayEliminationEffect(eliminatedCell.RectTransform.position)));
                }
            }
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
            LayoutSlotViews();
            TriggerGameResultEvent(gameState);
            TryUpdateSlotLayout();
        }

        /// <summary>
        /// 在所有已开始的入槽和消除动画结束后，按最终逻辑卡槽顺序重排视图。
        /// </summary>
        private void TryUpdateSlotLayout()
        {
            if (movingCardIds.Count != 0 || pendingEliminationResults.Count != 0 || activeEliminationCount != 0)
            {
                return;
            }

            LayoutSlotViews();
            RefreshBoardStates();
            if (gameLogic.Model.State == MahjongGameState.Lost)
            {
                TriggerGameResultEvent(gameLogic.Model.State);
            }
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
            DOVirtual.DelayedCall(2f, () => RecycleEliminationEffect(effect))
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
        /// 按指定卡槽顺序重新排列所有槽内卡牌视图；未指定时使用当前逻辑卡槽顺序。
        /// </summary>
        private void LayoutSlotViews(IReadOnlyList<int> cardInstanceIds = null)
        {
            IReadOnlyList<int> slotCardIds = cardInstanceIds ?? gameLogic.Model.Slot.CardInstanceIds;
            int displayIndex = 0;
            for (int i = 0; i < slotCardIds.Count; i++)
            {
                int cardInstanceId = slotCardIds[i];
                MahjongCardModel card = gameLogic.Model.GetCard(cardInstanceId);
                if (card.State == MahjongCardState.PendingElimination)
                {
                    continue;
                }

                if (cellViews.TryGetValue(cardInstanceId, out MahjongCell slotCell))
                {
                    Vector2 targetPosition = GetSlotPosition(displayIndex);
                    if (movingCardIds.Contains(cardInstanceId))
                    {
                        StartSlotMove(cardInstanceId, slotCell.RetargetToSlotPosition(targetPosition));
                    }
                    else
                    {
                        slotCell.AnimateSlotReposition(targetPosition);
                    }
                }

                displayIndex++;
            }
        }

        /// <summary>
        /// 将插入点之后的卡牌移动到新槽位。飞行中的卡牌会从当前位置重定向并保持入槽完成回调。
        /// </summary>
        private void AnimateSlotCellsAfterInsertion(IReadOnlyList<int> slotCardIdsBeforeElimination, int slotInsertIndex)
        {
            for (int i = slotInsertIndex + 1; i < slotCardIdsBeforeElimination.Count; i++)
            {
                int cardInstanceId = slotCardIdsBeforeElimination[i];
                if (!cellViews.TryGetValue(cardInstanceId, out MahjongCell slotCell))
                {
                    continue;
                }

                Vector2 targetPosition = GetSlotPosition(i);
                if (movingCardIds.Contains(cardInstanceId))
                {
                    StartSlotMove(cardInstanceId, slotCell.RetargetToSlotPosition(targetPosition));
                }
                else
                {
                    slotCell.AnimateSlotReposition(targetPosition);
                }
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
                               gameLogic.IsCardCovered(card.InstanceId) ||
                               gameLogic.HasBothSideNeighbors(card.InstanceId);
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
        private static Vector2 GetBoardPosition(MahjongGridPosition position, int layer, MahjongLevelDefinition levelDefinition)
        {
            float maxColumn = levelDefinition.gridColumnCount - 1;
            float maxRow = levelDefinition.gridRowCount - 1;
            float offset = MahjongLayoutGeometry.IsOffsetLayer(layer) ? 0.5f : 0f;
            float layerVisualOffset = layer / 2 * MahjongViewConfig.LayerVisualOffsetX;
            float x = (position.Column + offset - maxColumn * 0.5f) * MahjongViewConfig.BoardCellWidth /
                      MahjongConfig.GridCoordinateStep - layerVisualOffset;
            float y = (position.Row + offset - maxRow * 0.5f) * MahjongViewConfig.BoardCellHeight /
                      MahjongConfig.GridCoordinateStep;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 获取卡牌进入卡槽时应插入的索引：同类型卡牌存在时紧随最后一张，否则追加至末尾。
        /// </summary>
        private int GetSlotInsertIndex(int cardInstanceId)
        {
            MahjongCardModel card = gameLogic.Model.GetCard(cardInstanceId);
            for (int i = gameLogic.Model.Slot.Count - 1; i >= 0; i--)
            {
                MahjongCardModel slotCard = gameLogic.Model.GetCard(gameLogic.Model.Slot.CardInstanceIds[i]);
                if (slotCard.TypeId == card.TypeId)
                {
                    return i + 1;
                }
            }

            return gameLogic.Model.Slot.Count;
        }

        /// <summary>
        /// 根据卡槽索引计算对应的 UGUI 局部坐标。调用前必须保证索引位于卡槽容量范围内。
        /// </summary>
        private static Vector2 GetSlotPosition(int slotIndex)
        {
            float centeredIndex = slotIndex - (MahjongConfig.SlotCapacity - 1) * 0.5f;
            return new Vector2(centeredIndex * MahjongViewConfig.SlotCellWidth, 0f);
        }

    }
}
