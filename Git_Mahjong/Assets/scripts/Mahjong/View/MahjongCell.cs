using DG.Tweening;
using MahjongGame.Model;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MahjongGame.View
{
    /// <summary>
    /// 麻将卡牌可切换的背景样式。
    /// </summary>
    public enum MahjongCellBackgroundStyle
    {
        Default,
        Green,
        Yellow,
        Red
    }

    /// <summary>
    /// 单张麻将卡牌的 UGUI 交互视图。
    /// </summary>
    public sealed class MahjongCell : MonoBehaviour, IPointerClickHandler
    {
        public AudioSource audioSource;
        [SerializeField] private Image backgroundImage; // 卡牌背景图片
        [SerializeField] private Sprite defaultBackgroundSprite; // 默认卡牌背景图片
        [SerializeField] private Sprite greenBackgroundSprite; // 绿色卡牌背景图片
        [SerializeField] private Sprite yellowBackgroundSprite; // 黄色卡牌背景图片
        [SerializeField] private Sprite redBackgroundSprite; // 红色卡牌背景图片
        [SerializeField] private Image iconImage; // 卡牌类型图标
        [SerializeField] private Text typeText; // 缺少图标时显示的卡牌类型数字文本
        [SerializeField] private CanvasGroup canvasGroup; // 卡牌透明度与交互控制
        [SerializeField] private GameObject BlockedMaskObj; // 被阻挡的牌的mask遮罩

        private GameObject hintEffect; // 提示特效节点
        private GameObject selectionEffect; // 选中特效节点
        private RectTransform rectTransform; // 当前卡牌矩形变换
        private Action<MahjongCell> selectRequested; // 卡牌选择事件
        private bool isPointerEnabled; // 当前是否允许接收点击与射线

        public int InstanceId { get; private set; } // 卡牌唯一实例ID
        public int TypeId { get; private set; } // 卡牌类型ID
        public RectTransform RectTransform => rectTransform; // 卡牌矩形变换只读引用

        /// <summary>
        /// 缓存当前卡牌的矩形变换引用。
        /// </summary>
        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            hintEffect = transform.Find("HintEffect").gameObject;
            selectionEffect = transform.Find("SelectionEffect").gameObject;
        }

        /// <summary>
        /// 初始化卡牌视图。调用前必须提供有效数据、拖拽区域、卡槽区域与选择回调。
        /// </summary>
        public void Initialize(
            MahjongCardModel card,
            Sprite displaySprite,
            Color fallbackColor,
            Action<MahjongCell> selectRequested)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            this.selectRequested = selectRequested ?? throw new ArgumentNullException(nameof(selectRequested));
            InstanceId = card.InstanceId;
            TypeId = card.TypeId;
            transform.localScale = Vector3.one;
            canvasGroup.alpha = MahjongViewConfig.NormalAlpha;
            SetBackgroundStyle(MahjongCellBackgroundStyle.Default);
            bool hasDisplaySprite = displaySprite != null;
            backgroundImage.color = hasDisplaySprite ? Color.white : fallbackColor;
            iconImage.sprite = displaySprite;
            iconImage.SetNativeSize();
            iconImage.gameObject.SetActive(hasDisplaySprite);
            typeText.gameObject.SetActive(!hasDisplaySprite);
            typeText.text = TypeId.ToString();
            name = hasDisplaySprite ? iconImage.sprite.name : TypeId.ToString();
            SetInteractable(true);
            BlockedMaskObj.SetActive(false);
            SetHintEffectActive(false);
            SetSelectionEffectActive(false);
        }

        /// <summary>
        /// 更新卡牌显示的类型图标和颜色。调用前必须已完成 Initialize。
        /// </summary>
        public void RefreshVisual(Sprite displaySprite, Color fallbackColor)
        {
            bool hasDisplaySprite = displaySprite != null;
            backgroundImage.color = hasDisplaySprite ? Color.white : fallbackColor;
            iconImage.sprite = displaySprite;
            iconImage.gameObject.SetActive(hasDisplaySprite);
            typeText.gameObject.SetActive(!hasDisplaySprite);
            typeText.text = TypeId.ToString();
            name = hasDisplaySprite ? iconImage.sprite.name : TypeId.ToString();
        }

        /// <summary>
        /// 设置提示特效节点是否显示。
        /// </summary>
        public void SetHintEffectActive(bool active)
        {
            hintEffect.SetActive(active);
        }

        /// <summary>
        /// 设置选中特效节点是否显示。
        /// </summary>
        public void SetSelectionEffectActive(bool active)
        {
            selectionEffect.SetActive(active);
        }

        /// <summary>
        /// 切换卡牌背景样式。调用前必须由预制体配置对应背景图片。
        /// </summary>
        public void SetBackgroundStyle(MahjongCellBackgroundStyle style)
        {
            switch (style)
            {
                case MahjongCellBackgroundStyle.Green:
                    backgroundImage.sprite = greenBackgroundSprite;
                    break;
                case MahjongCellBackgroundStyle.Yellow:
                    backgroundImage.sprite = yellowBackgroundSprite;
                    break;
                case MahjongCellBackgroundStyle.Red:
                    backgroundImage.sprite = redBackgroundSprite;
                    break;
                default:
                    backgroundImage.sprite = defaultBackgroundSprite;
                    break;
            }
        }

        /// <summary>
        /// 记录卡牌牌面位置。调用前必须先完成 Initialize。
        /// </summary>
        public void SetBoardPosition(Vector2 position)
        {
            rectTransform.anchoredPosition = position;
        }

        /// <summary>
        /// 重置并回收卡牌视图。调用前必须确保该卡牌已从活动视图映射中移除。
        /// </summary>
        public void ResetForPool(Transform poolParent)
        {
            if (poolParent == null)
            {
                throw new ArgumentNullException(nameof(poolParent));
            }

            DOTween.Kill(this);
            transform.SetParent(poolParent, false);
            transform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            canvasGroup.alpha = MahjongViewConfig.NormalAlpha;
            canvasGroup.blocksRaycasts = false;
            InstanceId = 0;
            TypeId = 0;
            selectRequested = null;
            isPointerEnabled = false;
            gameObject.SetActive(false);
            BlockedMaskObj.SetActive(false);
            SetHintEffectActive(false);
            SetSelectionEffectActive(false);
        }

        /// <summary>
        /// 设置卡牌是否允许点击和拖拽，并同步射线检测状态。
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isPointerEnabled = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        /// <summary>
        /// 设置卡牌被阻挡时的显示状态。调用前必须保证卡牌仍在牌面上。
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            BlockedMaskObj.gameObject.SetActive(blocked);
            canvasGroup.alpha = blocked ? MahjongViewConfig.BlockedAlpha : MahjongViewConfig.NormalAlpha;
            isPointerEnabled = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 播放非法操作抖动并返回原位。调用前必须保证卡牌未进入卡槽。
        /// </summary>
        public Sequence AnimateRejected()
        {
            DOTween.Kill(this);
            return DOTween.Sequence()
                .Append(transform.DOShakePosition(
                    0.3f,
                    new Vector3(8f, 0f, 0f),
                    20,
                    0f,
                    false,
                    false))
                .SetTarget(this);
        }

        /// <summary>
        /// 从当前牌面位置移动至中转点，再按指定时序执行两次碰撞。
        /// </summary>
        public Tween AnimateToEliminationPoint(
            Transform parent,
            Vector2 transitPosition,
            Vector2 firstCollisionPosition,
            Vector2 reboundPosition,
            Vector2 secondCollisionPosition)
        {
            transform.SetParent(parent, true);
            canvasGroup.alpha = MahjongViewConfig.NormalAlpha;
            return DOTween.Sequence()
                .Append(rectTransform.DOAnchorPos(transitPosition, MahjongViewConfig.MoveToTransitDuration)
                    .SetEase(Ease.OutQuad))
                .Append(rectTransform.DOAnchorPos(firstCollisionPosition, MahjongViewConfig.MoveToCenterDuration)
                    .SetEase(Ease.InQuad))
                .Append(rectTransform.DOAnchorPos(reboundPosition, MahjongViewConfig.ReboundDuration)
                    .SetEase(Ease.OutQuad))
                .Append(rectTransform.DOAnchorPos(secondCollisionPosition, MahjongViewConfig.SecondCollisionDuration)
                    .SetEase(Ease.InQuad))
                .SetTarget(this);
        }

        /// <summary>
        /// 播放卡牌消除动画。调用前必须由业务结果确认卡牌已经消除。
        /// </summary>
        public Tween AnimateEliminated(Action eliminationCompleted)
        {
            AudioManager.Instance.PlayMahjongCellMusic("xiaochu");
            SetInteractable(false);
            return transform.DOScale(Vector3.zero, MahjongViewConfig.EliminateDuration)
                .OnComplete(() => eliminationCompleted?.Invoke())
                .SetEase(Ease.InBack)
                .SetTarget(this);
        }

        /// <summary>
        /// 处理左键点击并派发选择事件。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isPointerEnabled || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            
            AudioManager.Instance.PlayMahjongCellMusic("btn");
            selectRequested(this);
        }

        /// <summary>
        /// 销毁卡牌视图时停止以该视图为目标的补间动画。
        /// </summary>
        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }

    /// <summary>
    /// 麻将 UGUI 视图层的静态表现配置。
    /// </summary>
    public static class MahjongViewConfig
    {
        public const int InitialHealth = 3; // 每关初始生命数
        public const float NormalAlpha = 1f; // 可操作卡牌透明度
        public const float BlockedAlpha = 1f; // 被阻挡卡牌透明度
        public const float TransitOffsetX = 300f; // 中转点相对碰撞中心的横向距离
        public const float CollisionHalfDistance = 85.5f; // 两张完整卡牌刚好触碰时中心到碰撞点的横向距离
        public const float ReboundOffsetX = 60f; // 首次碰撞后的横向回弹距离
        public const float MoveToTransitDuration = 0.22f; // 卡牌移动至中转点的动画时长
        public const float MoveToCenterDuration = 0.16f; // 卡牌首次碰撞的动画时长
        public const float ReboundDuration = 0.1f; // 卡牌首次碰撞后的回弹时长
        public const float SecondCollisionDuration = 0.12f; // 卡牌第二次碰撞的动画时长
        public const float EliminateDuration = 0.2f; // 卡牌消除动画时长
        public const float EliminationEffectDuration = 2f; // 消除粒子完整播放及回收时长
        public const float HealthFadeDuration = 0.25f; // 扣除生命时的淡出时长
        public const float BoardCellWidth = 163f; // 牌面完整网格横向间距
        public const float BoardCellHeight = 190f; // 牌面完整网格纵向间距
        public const float LayerVisualOffsetX = 9f; // 每升一层额外增加的向左X轴视觉偏移
    }
}
