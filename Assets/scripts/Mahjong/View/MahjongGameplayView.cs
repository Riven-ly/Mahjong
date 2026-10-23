using System.Collections.Generic;
using CashRewards.ComboEffects;
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
        private const int SpecialCardTypeId = 999; // 特殊麻将的类型ID

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
        private int comboCount;
        private MahjongCellBackgroundStyle currentBackgroundStyle;
        private readonly List<MatchNewEffectPool> matchNewEffectPools = new List<MatchNewEffectPool>(); // 按颜色模板划分的可复用匹配特效池

        [SerializeField] private ComboEffectPlayer comboEffects;
        [SerializeField] private MatchEffectPlayer matchEffect;
        [SerializeField] private GameObject matchNewEffect_Blue;
        [SerializeField] private GameObject matchNewEffect_Green;
        [SerializeField] private GameObject matchNewEffect_Yellow;
        [SerializeField] private GameObject matchNewEffect_Pink;

        [SerializeField] private float luckyLevelPlaySeconds; // 本关累计的正常游戏秒数，换关清零
        private float luckyLastCheckTime; // 上次结算 Lucky 规则的全局时间戳
        private static float luckyLastShowTime = float.NegativeInfinity; // 上次 Lucky 奖励弹出的全局时间戳，换关不清零

        private float idleHintTimer; // 闲置计时器，玩家操作或高亮后清零
        private float idleHintLastCheckTime; // 上次结算闲置引导的全局时间戳
        private bool idleHintActive; // 当前是否正在显示闲置高亮

        private static float remoteIdleHintWaitSeconds = MahjongConfig.IdleHintWaitSeconds; // 闲置等待秒数，可被远程配置覆盖
        private static float remoteIdleHintActiveSeconds = MahjongConfig.IdleHintActiveSeconds; // 闲置高亮持续秒数，可被远程配置覆盖

        private const float MahjongCellHalfWidth = 85.5f; // 单张麻将牌局部坐标宽度的一半
        private const float MahjongCellHalfHeight = 99.5f; // 单张麻将牌局部坐标高度的一半
        private const float HorizontalScreenMargin = 50f; // 屏幕安全区左右保留边距
        private const float EntranceCellPitchX = MahjongViewConfig.BoardCellWidth * 0.5f; // 相邻列卡牌的水平间距，用于入场扩散距离
        private const float EntranceCellPitchY = MahjongViewConfig.BoardCellHeight * 0.5f; // 相邻行卡牌的垂直间距，用于入场扩散距离
        private const int Combo5EffectTriggerCount = 5; // 五连击特效触发所需连击数

        public event System.Action<int> HealthChanged; // 剩余生命变化事件
        public event System.Action<int> RemainingPairsChanged;

        public static float RemoteIdleHintWaitSeconds
        {
            set => remoteIdleHintWaitSeconds = value > 0f ? value : MahjongConfig.IdleHintWaitSeconds;
        }

        public static float RemoteIdleHintActiveSeconds
        {
            set => remoteIdleHintActiveSeconds = value > 0f ? value : MahjongConfig.IdleHintActiveSeconds;
        }

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
            luckyLevelPlaySeconds = 0f;
            luckyLastCheckTime = Time.time;
            selectedCardId = 0;
            comboCount = 0;
            currentBackgroundStyle = (MahjongCellBackgroundStyle)Random.Range(0, 4);
            SetMatchNewEffectsActive(false);
            remainingHealth = MahjongViewConfig.InitialHealth;
            autoEliminationRemainingGroupCount = 0;
            idleHintTimer = 0f;
            idleHintActive = false;
            idleHintLastCheckTime = Time.time;
            ApplyBoardScale(levelDefinition);
            MahjongOperationResult result = gameLogic.StartNewGame(levelDefinition);
            BuildBoardViews();
            MarkSpecialPair();
            EnsurePlayablePair();
            PlayEntranceAnimation();
            RemainingPairsChanged?.Invoke(CountRemainingPairs());
        }

        private int CountRemainingPairs()
        {
            int remainingCardCount = 0;
            for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
            {
                if (gameLogic.Model.Cards[i].State == MahjongCardState.OnBoard)
                {
                    remainingCardCount++;
                }
            }

            return remainingCardCount / MahjongConfig.MatchCount;
        }

        /// <summary>
        /// 播放关卡创建完成后的卡牌入场动画：以牌面左上角为原点，按对角距离由近及远依次淡入。
        /// </summary>
        private void PlayEntranceAnimation()
        {
            if (cellViews.Count == 0)
            {
                return;
            }

            float originX = float.MaxValue;
            float originY = float.MinValue;
            foreach (KeyValuePair<int, MahjongCell> pair in cellViews)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                Vector2 position = pair.Value.RectTransform.anchoredPosition;
                originX = Mathf.Min(originX, position.x);
                originY = Mathf.Max(originY, position.y);
            }

            float totalDuration = MahjongViewConfig.EntranceDuration;
            SetBoardInput(false);
            foreach (KeyValuePair<int, MahjongCell> pair in cellViews)
            {
                MahjongCell cell = pair.Value;
                if (cell == null)
                {
                    continue;
                }

                Vector2 position = cell.RectTransform.anchoredPosition;
                float distance = Mathf.Abs(position.x - originX) / EntranceCellPitchX
                                 + Mathf.Abs(originY - position.y) / EntranceCellPitchY;
                float delay = distance * MahjongViewConfig.EntranceStagger;
                totalDuration = Mathf.Max(totalDuration, delay + MahjongViewConfig.EntranceDuration);
                cell.AnimateEntrance(MahjongViewConfig.EntranceDuration, delay);
            }

            DOVirtual.DelayedCall(totalDuration, () => SetBoardInput(true)).SetTarget(this);
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
                          card.layer * MahjongViewConfig.LayerVisualOffsetX;
                float y = (card.coordX * 0.5f - (levelDefinition.gridRowCount - 1) * 0.5f) * MahjongViewConfig.BoardCellHeight +
                          card.layer * MahjongViewConfig.LayerVisualOffsetY;
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
            StopIdleHint();
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
            ResetMatchNewEffects();
            if (autoEliminationRemainingGroupCount > 0)
            {
                EndAutoElimination();
            }

            selectedCardId = 0;
            activeEliminationCount = 0;
            comboEffects?.Hide();
        }

        /// <summary>
        /// 【调试】直接完成当前关卡：清空剩余卡牌并触发胜利结算。
        /// </summary>
        public void DebugCompleteLevel()
        {
            if (gameLogic == null || gameLogic.Model == null || gameLogic.Model.State != MahjongGameState.Playing)
            {
                Debug.LogWarning("[Debug] 当前不在进行中的牌局，无法完成关卡");
                return;
            }

            List<int> remainingCardIds = new List<int>();
            for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
            {
                MahjongCardModel card = gameLogic.Model.Cards[i];
                if (card.State == MahjongCardState.OnBoard)
                {
                    remainingCardIds.Add(card.InstanceId);
                }
            }

            if (remainingCardIds.Count == 0)
            {
                return;
            }

            StopHint();
            DOTween.Kill(this);
            activeEliminationCount = 0;
            selectedCardId = 0;
            autoEliminationRemainingGroupCount = 0;

            for (int i = 0; i < remainingCardIds.Count / MahjongConfig.MatchCount; i++)
            {
                PhaseTaskService.NotifyArrowEscaped();
            }

            MahjongGameState gameState = gameLogic.CompleteElimination(remainingCardIds);
            ClearCellViews();
            TriggerGameResultEvent(gameState);
        }

        /// <summary>
        /// 【调试】重播当前牌面的卡牌入场动画。
        /// </summary>
        public void DebugReplayEntrance()
        {
            if (cellViews.Count == 0)
            {
                Debug.LogWarning("[Debug] 当前没有牌面视图，无法重播入场动画");
                return;
            }

            PlayEntranceAnimation();
        }

        /// <summary>
        /// 根据当前逻辑数据构建整個牌面视图。调用前必须已开始新游戏。
        /// </summary>
        private void BuildBoardViews()
        {
            int layerCount = gameLogic.Model.LevelDefinition.GetLayerCount();
            for (int layer = 0; layer < layerCount; layer++)
            {
                int shadowSortingOrder = GetLayerSortingOrderStart(layer);
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
                    cell.SetBackgroundStyle(currentBackgroundStyle);
                    cell.SetBoardPosition(GetBoardPosition(card, gameLogic.Model.LevelDefinition));
                    cell.SetSortingOrder(GetSortingOrder(card, gameLogic.Model.LevelDefinition));
                    cell.SetShadowSortingOrder(shadowSortingOrder);
                    cell.transform.SetAsLastSibling();
                    cellViews.Add(card.InstanceId, cell);
                }
            }
        }

        /// <summary>
        /// 从牌面随机挑一组同类型牌改造成特殊麻将。仅真实关卡生效。
        /// </summary>
        private void MarkSpecialPair()
        {
            if (TxGameBridge.CurrentLevel < 2)
            {
                return;
            }

            List<MahjongCardModel> boardCards = new List<MahjongCardModel>();
            for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
            {
                MahjongCardModel card = gameLogic.Model.Cards[i];
                if (card.State == MahjongCardState.OnBoard)
                {
                    boardCards.Add(card);
                }
            }

            if (boardCards.Count < MahjongConfig.MatchCount)
            {
                return;
            }

            MahjongCardModel firstCard = boardCards[Random.Range(0, boardCards.Count)];
            List<MahjongCardModel> partners = new List<MahjongCardModel>();
            for (int i = 0; i < boardCards.Count; i++)
            {
                if (boardCards[i].InstanceId != firstCard.InstanceId && boardCards[i].TypeId == firstCard.TypeId)
                {
                    partners.Add(boardCards[i]);
                }
            }

            if (partners.Count == 0)
            {
                return;
            }

            SetSpecialCard(firstCard);
            SetSpecialCard(partners[Random.Range(0, partners.Count)]);
        }

        /// <summary>
        /// 将指定卡牌改造成特殊麻将并同步视图表现。
        /// </summary>
        private void SetSpecialCard(MahjongCardModel card)
        {
            card.SetTypeId(SpecialCardTypeId);
            if (cellViews.TryGetValue(card.InstanceId, out MahjongCell cell))
            {
                cell.SetSpecial(true);
            }
        }

        /// <summary>
        /// 处理卡牌视图的点击选择、取消选择或配对消除。
        /// </summary>
        private void HandleCellSelectRequested(MahjongCell cell)
        {
            ResetIdleHintTimer();
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
                comboCount = 0;
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
                }

                return;
            }

            StopHint();
            firstCell.SetSelectionEffectActive(true);
            cell.SetSelectionEffectActive(true);
            PlayEliminationAnimation(result);
        }

        private void PlayComboEffect(Vector3 worldPosition)
        {
            if (comboEffects == null)
            {
                return;
            }

            comboEffects.transform.position = worldPosition;
            comboEffects.PlayCombo(comboCount);
        }

        /// <summary>
        /// 单一颜色匹配特效的对象池。
        /// </summary>
        private class MatchNewEffectPool
        {
            public GameObject Template;
            public readonly Stack<GameObject> Idle = new Stack<GameObject>();
            public readonly List<GameObject> Instances = new List<GameObject>();
        }

        /// <summary>
        /// 按当前关卡的卡牌背景颜色在指定世界坐标播放一次匹配特效。
        /// </summary>
        private void PlayMatchNewEffect(Vector3 worldPosition)
        {
            GameObject template = GetMatchNewEffect(currentBackgroundStyle);
            if (template == null)
            {
                return;
            }

            MatchNewEffectPool pool = GetMatchNewEffectPool(template);
            GameObject effect = pool.Idle.Count > 0 ? pool.Idle.Pop() : CreateMatchNewEffect(pool);
            effect.transform.position = worldPosition;
            effect.SetActive(true);
            DOVirtual.DelayedCall(
                    MahjongViewConfig.EliminationEffectDuration,
                    () => RecycleMatchNewEffect(pool, effect))
                .SetTarget(effect);
        }

        /// <summary>
        /// 取得指定模板对应的特效对象池，首次使用时以模板本身作为初始实例。
        /// </summary>
        private MatchNewEffectPool GetMatchNewEffectPool(GameObject template)
        {
            for (int i = 0; i < matchNewEffectPools.Count; i++)
            {
                if (matchNewEffectPools[i].Template == template)
                {
                    return matchNewEffectPools[i];
                }
            }

            MatchNewEffectPool pool = new MatchNewEffectPool { Template = template };
            pool.Instances.Add(template);
            pool.Idle.Push(template);
            matchNewEffectPools.Add(pool);
            return pool;
        }

        /// <summary>
        /// 实例化一份匹配特效并纳入对应对象池。
        /// </summary>
        private GameObject CreateMatchNewEffect(MatchNewEffectPool pool)
        {
            GameObject effect = Instantiate(pool.Template, pool.Template.transform.parent);
            effect.SetActive(false);
            pool.Instances.Add(effect);
            return effect;
        }

        /// <summary>
        /// 失活并回收指定匹配特效。
        /// </summary>
        private void RecycleMatchNewEffect(MatchNewEffectPool pool, GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            DOTween.Kill(effect);
            effect.SetActive(false);
            if (!pool.Idle.Contains(effect))
            {
                pool.Idle.Push(effect);
            }
        }

        /// <summary>
        /// 失活并回收全部匹配特效。面板隐藏或重建游戏前调用。
        /// </summary>
        private void ResetMatchNewEffects()
        {
            for (int i = 0; i < matchNewEffectPools.Count; i++)
            {
                MatchNewEffectPool pool = matchNewEffectPools[i];
                pool.Idle.Clear();
                for (int j = 0; j < pool.Instances.Count; j++)
                {
                    GameObject instance = pool.Instances[j];
                    if (instance == null)
                    {
                        continue;
                    }

                    DOTween.Kill(instance);
                    instance.SetActive(false);
                    pool.Idle.Push(instance);
                }
            }
        }

        private GameObject GetMatchNewEffect(MahjongCellBackgroundStyle style)
        {
            switch (style)
            {
                case MahjongCellBackgroundStyle.Green:
                    return matchNewEffect_Green;
                case MahjongCellBackgroundStyle.Yellow:
                    return matchNewEffect_Yellow;
                case MahjongCellBackgroundStyle.Red:
                    return matchNewEffect_Pink;
                default:
                    return matchNewEffect_Blue;
            }
        }

        private void SetMatchNewEffectsActive(bool active)
        {
            if (matchNewEffect_Blue != null)
            {
                matchNewEffect_Blue.SetActive(active);
            }

            if (matchNewEffect_Green != null)
            {
                matchNewEffect_Green.SetActive(active);
            }

            if (matchNewEffect_Yellow != null)
            {
                matchNewEffect_Yellow.SetActive(active);
            }

            if (matchNewEffect_Pink != null)
            {
                matchNewEffect_Pink.SetActive(active);
            }
        }

        /// <summary>
        /// 打开五连击左右金色光效。调用前必须确保特效节点已在场景中配置。
        /// </summary>
        private void PlayCombo5Effect()
        {
            GameObject effect = GameManager.Instance.Combo5Effect;
            if (effect == null)
            {
                return;
            }

            effect.SetActive(false);
            effect.SetActive(true);
        }

        private void PlayComboCoinFly(Vector3 worldPosition, int combo)
        {
            int goldAmount = combo * 10;
            GameManager.Instance.playerInfo.Add_gold(goldAmount);
            int coinCount = 4;
            PlayerInfoUI ui = UIManager.Instance.GetUI<PlayerInfoUI>();
            if (ui != null)
            {
                ui.ComboGoldFlyAnim(worldPosition, coinCount);
            }
        }

        /// <summary>
        /// 随机交换游戏区域卡牌的完整棋盘位置并刷新牌面显示。
        /// </summary>
        public bool TryShuffle()
        {
            ResetIdleHintTimer();
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

            comboCount = 0;
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

            while (CountRemainingPairs() > 0 && gameLogic.GetHintCardIds().Count == 0)
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
            ResetIdleHintTimer();
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
            ResetIdleHintTimer();
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
        /// 显示首关引导所需的一组提示牌，并返回两张目标牌信息。
        /// </summary>
        public bool TryShowFirstGameGuide(
            out int firstTargetCardId,
            out RectTransform firstTargetRectTransform,
            out int secondTargetCardId,
            out RectTransform secondTargetRectTransform)
        {
            firstTargetCardId = 0;
            firstTargetRectTransform = null;
            secondTargetCardId = 0;
            secondTargetRectTransform = null;
            if (!IsStable())
            {
                return false;
            }

            IReadOnlyList<int> hintCardIds = gameLogic.GetHintCardIds();
            if (hintCardIds.Count < 2 ||
                !cellViews.TryGetValue(hintCardIds[0], out MahjongCell firstTargetCell) ||
                !cellViews.TryGetValue(hintCardIds[1], out MahjongCell secondTargetCell))
            {
                return false;
            }

            firstTargetCell.SetHintEffectActive(true);
            secondTargetCell.SetHintEffectActive(true);
            firstTargetCardId = firstTargetCell.InstanceId;
            firstTargetRectTransform = firstTargetCell.RectTransform;
            secondTargetCardId = secondTargetCell.InstanceId;
            secondTargetRectTransform = secondTargetCell.RectTransform;
            return true;
        }

        /// <summary>
        /// 转发首关引导目标牌的点击操作。
        /// </summary>
        public void SelectFirstGameGuideCard(int cardInstanceId)
        {
            if (cellViews.TryGetValue(cardInstanceId, out MahjongCell cell))
            {
                HandleCellSelectRequested(cell);
            }
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
                int shadowSortingOrder = GetLayerSortingOrderStart(layer);
                for (int i = 0; i < gameLogic.Model.Cards.Count; i++)
                {
                    MahjongCardModel card = gameLogic.Model.Cards[i];
                    if (card.State != MahjongCardState.OnBoard || card.Layer != layer ||
                        !cellViews.TryGetValue(card.InstanceId, out MahjongCell cell))
                    {
                        continue;
                    }

                    cell.SetBoardPosition(GetBoardPosition(card, gameLogic.Model.LevelDefinition));
                    cell.SetSortingOrder(GetSortingOrder(card, gameLogic.Model.LevelDefinition));
                    cell.SetShadowSortingOrder(shadowSortingOrder);
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
            MahjongCell firstCell = cellViews[result.EliminatedCardIds[0]];
            MahjongCell secondCell = cellViews[result.EliminatedCardIds[1]];
            firstCell.SetInteractable(false);
            secondCell.SetInteractable(false);
            firstCell.SetSortingOrder(MahjongViewConfig.SortingOrderEliminating);
            secondCell.SetSortingOrder(MahjongViewConfig.SortingOrderEliminating);
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

            float boardScale = boardRoot.lossyScale.x / eliminationLayer.lossyScale.x;
            Vector2 firstCollisionPosition = centerPosition + Vector2.left * (MahjongViewConfig.CollisionHalfDistance * boardScale);
            Vector2 secondCollisionPosition = centerPosition + Vector2.right * (MahjongViewConfig.CollisionHalfDistance * boardScale);
            Vector2 firstReboundPosition = firstCollisionPosition + Vector2.left * (MahjongViewConfig.ReboundOffsetX * boardScale);
            Vector2 secondReboundPosition = secondCollisionPosition + Vector2.right * (MahjongViewConfig.ReboundOffsetX * boardScale);
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
                // PlayEliminationEffect(collisionWorldPosition);
                PlayMatchNewEffect(collisionWorldPosition);
                if (SettingPanel.IsVibrateEnabled)
                {
                    VibrationManager.Vibrate();
                }

                comboCount++;
                PlayComboEffect(collisionWorldPosition);
                PlayComboCoinFly(collisionWorldPosition, comboCount);
                if (comboCount % Combo5EffectTriggerCount == 0)
                {
                    PlayCombo5Effect();
                }
                firstCell.AnimateEliminated(null);
                secondCell.AnimateEliminated(null);
            });
            eliminateSequence.AppendInterval(MahjongViewConfig.EliminateDuration);
            eliminateSequence.SetTarget(this).AppendCallback(() => CompleteEliminationAnimation(result));
        }


        /// <summary>
        /// 消除动画播放结束后结算本次消除；特殊麻将先弹奖励面板，其余逻辑等面板关闭后执行。
        /// </summary>
        private void CompleteEliminationAnimation(MahjongOperationResult result)
        {
            PhaseTaskService.NotifyArrowEscaped();
            if (gameLogic.Model.GetCard(result.EliminatedCardIds[0]).TypeId == SpecialCardTypeId)
            {
                UIManager.Instance.OpenUI<GeneralRewardsPanel>(null, () => FinishElimination(result));
                return;
            }

            FinishElimination(result);
        }

        /// <summary>
        /// 回收已完成消除动画的卡牌视图，并刷新卡槽布局和牌面状态。
        /// </summary>
        private void FinishElimination(MahjongOperationResult result)
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
            RemainingPairsChanged?.Invoke(CountRemainingPairs());
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
            // 原「累计消除数 + 概率」触发逻辑已停用，改由累计游戏时间规则触发（见 UpdateLuckyReward）。
            //if (eliminatedCardCount < MahjongConfig.RewardTriggerEliminatedCardCount)
            //{
            //    _callback?.Invoke();
            //    return;
            //}

            //int extraGroupCount = (eliminatedCardCount - MahjongConfig.RewardTriggerEliminatedCardCount) /
            //                      MahjongConfig.MatchCount;
            //float probability = Mathf.Min(
            //    1f,
            //    MahjongConfig.RewardInitialProbability +
            //    extraGroupCount * MahjongConfig.RewardProbabilityIncreasePerGroup);
            //if (Random.value >= probability)
            //{
            //    _callback?.Invoke();
            //    return;
            //}

            //eliminatedCardCount = 0;
            //bool isAutoEliminating = autoEliminationRemainingGroupCount > 0;
            //if (isAutoEliminating)
            //{
            //    UIManager.Instance.HideUIMask();
            //}

            //UIManager.Instance.OpenUI<GeneralRewardsPanel>(null, () =>
            //{
            //    if (isAutoEliminating && autoEliminationRemainingGroupCount > 0)
            //    {
            //        UIManager.Instance.OpenUIMask();
            //    }

            //    _callback?.Invoke();
            //});

            _callback?.Invoke();
        }

        /// <summary>
        /// 每秒检查一次玩法状态与界面遮挡情况，通过后累计本关游戏时间，并在达到门槛时判定 Lucky 奖励弹出。
        /// </summary>
        private void UpdateLuckyReward()
        {
            if (Time.time - luckyLastCheckTime < 1f)
            {
                return;
            }

            luckyLastCheckTime = Time.time;

            if (!IsPlaying() || !IsNormalPlaying())
            {
                return;
            }

            luckyLevelPlaySeconds += 1f;

            if (luckyLevelPlaySeconds < MahjongConfig.LuckyRewardLevelSeconds)
            {
                return;
            }

            if (!CanShowLuckyReward())
            {
                return;
            }

            ShowLuckyReward();
        }

        /// <summary>
        /// 判断主玩法是否处于进行中状态，且当前关卡已达到 Lucky 奖励的开启关卡。
        /// </summary>
        private bool IsPlaying()
        {
            if (GameManager.Instance == null || GameManager.Instance.playerInfo == null)
            {
                return false;
            }

            if (GameManager.Instance.playerInfo.level < MahjongConfig.LuckyRewardMinLevel)
            {
                return false;
            }

            return gameLogic != null
                && gameLogic.Model != null
                && gameLogic.Model.State == MahjongGameState.Playing;
        }

        /// <summary>
        /// 判断当前是否处于可正常游玩状态，即玩法进行中且未被广告或弹窗打断。
        /// </summary>
        private bool IsNormalPlaying()
        {
            if (UIManager.Instance == null || UIManager.Instance.uiMask.gameObject.activeSelf)
            {
                return false;
            }

            return !UIManager.Instance.CheckIstheUIopen();
        }

        /// <summary>
        /// 判断 Lucky 奖励当前是否具备弹出条件，条件不满足时保留秒数等待下一秒。
        /// </summary>
        private bool CanShowLuckyReward()
        {
            if (Time.time - luckyLastShowTime < MahjongConfig.LuckyRewardCooldownSeconds)
            {
                return false;
            }

            return activeEliminationCount == 0 && autoEliminationRemainingGroupCount == 0;
        }

        /// <summary>
        /// 弹出 Lucky 奖励并重置本关计时、刷新冷却。
        /// </summary>
        private void ShowLuckyReward()
        {
            OtherSdkManager.Instance.CustomEvent("bonus_page_enter");

            UIManager.Instance.OpenUI<GeneralRewardsPanel>(null, () =>
            {
                luckyLevelPlaySeconds = 0f;
                luckyLastShowTime = Time.time;
            });
        }

        /// <summary>
        /// 每帧累加闲置计时，达到阈值后高亮一对可消除牌，持续指定秒数后自动关闭并重新计时。
        /// </summary>
        private void UpdateIdleHint()
        {
            if (Time.time - idleHintLastCheckTime < 1f)
            {
                return;
            }
            idleHintLastCheckTime = Time.time;

            if (idleHintActive)
            {
                if (!IsNormalPlaying() || activeEliminationCount != 0)
                {
                    StopIdleHint();
                    return;
                }
                idleHintTimer += 1f;
                if (idleHintTimer >= remoteIdleHintActiveSeconds)
                {
                    StopIdleHint();
                }
                return;
            }

            if (IsFirstGameTutorialActive() || !IsNormalPlaying())
            {
                return;
            }

            if (gameLogic == null || gameLogic.Model == null || gameLogic.Model.State != MahjongGameState.Playing || activeEliminationCount != 0)
            {
                return;
            }

            idleHintTimer += 1f;
            if (idleHintTimer < remoteIdleHintWaitSeconds)
            {
                return;
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
                idleHintTimer = 0f;
                return;
            }

            StopHint();
            for (int i = 0; i < hintCardIds.Count; i++)
            {
                if (cellViews.TryGetValue(hintCardIds[i], out MahjongCell cell))
                {
                    cell.SetHintEffectActive(true);
                }
            }
            idleHintActive = true;
            idleHintTimer = 0f;
        }

        private static bool IsFirstGameTutorialActive()
        {
            return GameManager.Instance != null
                && GameManager.Instance.playerInfo != null
                && GameManager.Instance.playerInfo.level == 1
                && TutorialStageCatalogLoader.StageCount > 0;
        }

        /// <summary>
        /// 关闭闲置高亮并重新开始等待计时。
        /// </summary>
        private void StopIdleHint()
        {
            if (!idleHintActive)
            {
                return;
            }
            idleHintActive = false;
            idleHintTimer = 0f;
            foreach (KeyValuePair<int, MahjongCell> pair in cellViews)
            {
                pair.Value.SetHintEffectActive(false);
            }
        }

        /// <summary>
        /// 重置闲置计时器，在玩家点击、触摸等操作时调用。
        /// </summary>
        private void ResetIdleHintTimer()
        {
            if (idleHintActive)
            {
                StopIdleHint();
            }
            idleHintTimer = 0f;
            idleHintLastCheckTime = Time.time;
        }

        private void Update()
        {
            //UpdateLuckyReward();
            UpdateIdleHint();
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
                               gameLogic.IsCardCovered(card.InstanceId) ||
                               gameLogic.IsCardBlockedByBothSides(card.InstanceId);
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
            float layerVisualOffset = card.Layer * MahjongViewConfig.LayerVisualOffsetX;
            float layerVisualOffsetY = card.Layer * MahjongViewConfig.LayerVisualOffsetY;
            float x = (centerColumn - maxColumn * 0.5f) * MahjongViewConfig.BoardCellWidth - layerVisualOffset;
            float y = (centerRow - maxRow * 0.5f) * MahjongViewConfig.BoardCellHeight + layerVisualOffsetY;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 按层级与牌面位置换算卡牌画布排序层级：层越高越靠上，同层越靠屏幕下方越靠前，同排越靠右越靠前。
        /// </summary>
        private static int GetSortingOrder(MahjongCardModel card, MahjongLevelDefinition levelDefinition)
        {
            int maximumCoordX = levelDefinition.gridRowCount * MahjongLayoutGeometry.HalfGridUnitsPerCell
                                - MahjongLayoutGeometry.HalfGridUnitsPerCell;
            return GetLayerSortingOrderStart(card.Layer)
                   + 1
                   + (maximumCoordX - card.CoordX) * MahjongViewConfig.SortingOrderRowStep
                   + card.CoordY;
        }

        /// <summary>
        /// 获取指定层的固定起始排序层级，该层影子使用此层级，牌面从下一级开始。
        /// </summary>
        private static int GetLayerSortingOrderStart(int layer)
        {
            return MahjongViewConfig.SortingOrderBase
                   + layer * MahjongViewConfig.SortingOrderLayerStep;
        }


    }
}
