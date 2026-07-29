using System;
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
        [SerializeField] private MahjongCell mahjongCellPrefab; // 单张卡牌视图预制体

        private readonly Dictionary<int, MahjongCell> cellViews = new Dictionary<int, MahjongCell>(); // 实例ID到卡牌视图的映射
        private readonly Stack<MahjongCell> cellPool = new Stack<MahjongCell>(); // 可复用的麻将卡牌视图池
        private MahjongGameLogic gameLogic; // 当前主玩法业务逻辑入口
        private bool isAnimating; // 当前是否正在播放业务结果动画

        public event Action<MahjongGameState> GameStateChanged; // 游戏状态变化事件

        /// <summary>
        /// 初始化麻将主玩法业务逻辑。
        /// </summary>
        private void Awake()
        {
            gameLogic = new MahjongGameLogic();
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
            GameStateChanged?.Invoke(result.GameState);
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
            isAnimating = false;
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
        /// 处理卡牌视图的选择请求，并按业务结果驱动入槽、拒绝或消除动画。
        /// </summary>
        private void HandleCellSelectRequested(MahjongCell cell, bool isDraggedToSlot)
        {
            if (isAnimating)
            {
                cell.AnimateBack();
                return;
            }

            int previousSlotCount = gameLogic.Model.Slot.Count;
            MahjongOperationResult result = isDraggedToSlot
                ? gameLogic.DragCardToSlot(cell.InstanceId)
                : gameLogic.SelectCard(cell.InstanceId);
            if (!result.Succeeded)
            {
                isAnimating = true;
                SetBoardInput(false);
                Tween failureTween = isDraggedToSlot && result.Failure == MahjongOperationFailure.NoMatchingCardInSlot
                    ? cell.AnimateBack()
                    : cell.AnimateRejected();
                failureTween.OnComplete(() =>
                {
                    RefreshBoardStates();
                    isAnimating = false;
                });
                return;
            }

            isAnimating = true;
            SetBoardInput(false);
            cell.SetInteractable(false);
            Vector2 slotPosition = GetSlotPosition(previousSlotCount);
            Tween moveTween = isDraggedToSlot
                ? cell.AnimateDraggedTo(slotRoot, slotPosition)
                : cell.AnimateTo(slotRoot, slotPosition);
            var sequence = DOTween.Sequence().Append(moveTween);
            if (result.EliminatedCardIds.Count > 0)
            {
                var eliminateSequence = DOTween.Sequence();
                for (int i = 0; i < result.EliminatedCardIds.Count; i++)
                {
                    if (cellViews.TryGetValue(result.EliminatedCardIds[i], out MahjongCell eliminatedCell))
                    {
                        eliminateSequence.Join(eliminatedCell.AnimateEliminated());
                    }
                }

                sequence.Append(eliminateSequence);
            }

            sequence.SetTarget(this).OnComplete(() => CompleteSelection(result));
        }

        /// <summary>
        /// 完成选择动画后的视图回收、卡槽重排与状态通知。调用前必须保证结果动画已结束。
        /// </summary>
        private void CompleteSelection(MahjongOperationResult result)
        {
            for (int i = 0; i < result.EliminatedCardIds.Count; i++)
            {
                int eliminatedCardId = result.EliminatedCardIds[i];
                if (cellViews.TryGetValue(eliminatedCardId, out MahjongCell eliminatedCell))
                {
                    cellViews.Remove(eliminatedCardId);
                    RecycleCellView(eliminatedCell);
                }
            }
            LayoutSlotViews();
            RefreshBoardStates();
            isAnimating = false;
            GameStateChanged?.Invoke(result.GameState);
        }

        /// <summary>
        /// 按当前卡槽数据重新排列所有槽内卡牌视图。
        /// </summary>
        private void LayoutSlotViews()
        {
            for (int i = 0; i < gameLogic.Model.Slot.CardInstanceIds.Count; i++)
            {
                int cardInstanceId = gameLogic.Model.Slot.CardInstanceIds[i];
                if (cellViews.TryGetValue(cardInstanceId, out MahjongCell slotCell))
                {
                    slotCell.AnimateSlotReposition(GetSlotPosition(i));
                }
            }
        }

        /// <summary>
        /// 根据业务逻辑刷新牌面卡牌的遮挡、邻牌阻挡与交互表现。
        /// </summary>
        private void RefreshBoardStates()
        {
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
        /// 从对象池获取卡牌视图；池为空时实例化新对象。
        /// </summary>
        private MahjongCell GetCellView()
        {
            MahjongCell cell = cellPool.Count > 0
                ? cellPool.Pop()
                : Instantiate(mahjongCellPrefab, boardRoot);
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
        /// 根据卡槽索引计算对应的 UGUI 局部坐标。调用前必须保证索引位于卡槽容量范围内。
        /// </summary>
        private static Vector2 GetSlotPosition(int slotIndex)
        {
            float centeredIndex = slotIndex - (MahjongConfig.SlotCapacity - 1) * 0.5f;
            return new Vector2(centeredIndex * MahjongViewConfig.SlotCellWidth, 0f);
        }

    }
}
