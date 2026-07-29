using System;
using DG.Tweening;
using MahjongGame.Model;
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
    public sealed class MahjongCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image backgroundImage; // 卡牌背景图片
        [SerializeField] private Sprite defaultBackgroundSprite; // 默认卡牌背景图片
        [SerializeField] private Sprite greenBackgroundSprite; // 绿色卡牌背景图片
        [SerializeField] private Sprite yellowBackgroundSprite; // 黄色卡牌背景图片
        [SerializeField] private Sprite redBackgroundSprite; // 红色卡牌背景图片
        [SerializeField] private Image iconImage; // 卡牌类型图标
        [SerializeField] private Text typeText; // 缺少图标时显示的卡牌类型数字文本
        [SerializeField] private CanvasGroup canvasGroup; // 卡牌透明度与交互控制

        private RectTransform rectTransform; // 当前卡牌矩形变换
        private RectTransform dragArea; // 拖拽坐标转换区域
        private RectTransform slotArea; // 有效拖拽目标区域
        private Transform originalParent; // 拖拽或点击前父节点
        private int originalSiblingIndex; // 拖拽或点击前同级索引
        private Vector2 boardPosition; // 卡牌在牌面上的原始位置
        private Vector3 pointerWorldOffset; // 指针世界坐标与卡牌中心的偏移
        private Action<MahjongCell, bool> selectRequested; // 卡牌选择事件及是否由拖拽入槽
        private bool isPointerEnabled; // 当前是否允许接收点击与射线
        private bool isDragEnabled; // 当前是否允许拖拽
        private bool isDragging; // 当前是否正在拖拽
        private bool suppressClick; // 当前拖拽手势是否需要屏蔽点击

        public int InstanceId { get; private set; } // 卡牌唯一实例ID
        public int TypeId { get; private set; } // 卡牌类型ID
        public RectTransform RectTransform => rectTransform; // 卡牌矩形变换只读引用

        /// <summary>
        /// 缓存当前卡牌的矩形变换引用。
        /// </summary>
        private void Awake()
        {
            rectTransform = (RectTransform)transform;
        }

        /// <summary>
        /// 初始化卡牌视图。调用前必须提供有效数据、拖拽区域、卡槽区域与选择回调。
        /// </summary>
        public void Initialize(
            MahjongCardModel card,
            RectTransform dragArea,
            RectTransform slotArea,
            Sprite displaySprite,
            Color fallbackColor,
            Action<MahjongCell, bool> selectRequested)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            this.dragArea = dragArea != null ? dragArea : throw new ArgumentNullException(nameof(dragArea));
            this.slotArea = slotArea != null ? slotArea : throw new ArgumentNullException(nameof(slotArea));
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
            //name = $"MahjongCell_{InstanceId}";
            name = iconImage.sprite.name;
            SetInteractable(true);
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
            boardPosition = position;
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
            dragArea = null;
            slotArea = null;
            originalParent = null;
            originalSiblingIndex = 0;
            selectRequested = null;
            isPointerEnabled = false;
            isDragEnabled = false;
            isDragging = false;
            suppressClick = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 设置卡牌是否允许点击和拖拽，并同步射线检测状态。
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isPointerEnabled = interactable;
            isDragEnabled = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        /// <summary>
        /// 设置卡牌被阻挡时的显示状态。调用前必须保证卡牌仍在牌面上。
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            canvasGroup.alpha = blocked ? MahjongViewConfig.BlockedAlpha : MahjongViewConfig.NormalAlpha;
            isPointerEnabled = true;
            isDragEnabled = !blocked;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 将卡牌动画返回牌面原始位置。调用前必须保证卡牌对象仍有效。
        /// </summary>
        public Tween AnimateBack()
        {
            transform.SetParent(originalParent, true);
            return rectTransform.DOAnchorPos(boardPosition, MahjongViewConfig.ReturnDuration)
                .OnComplete(() =>
                {
                    transform.SetSiblingIndex(originalSiblingIndex);
                })
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
        }

        /// <summary>
        /// 播放非法操作抖动并返回原位。调用前必须保证卡牌未进入卡槽。
        /// </summary>
        public Sequence AnimateRejected()
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = boardPosition;
            return DOTween.Sequence()
                .Append(transform.DOShakePosition(
                    0.3f,
                    new Vector3(8f, 0f, 0f),
                    20,
                    0f,
                    false,
                    false))
                .AppendCallback(() => rectTransform.anchoredPosition = boardPosition)
                .SetTarget(this);
        }

        /// <summary>
        /// 将卡牌设置到指定父节点，沿前段轻微弯曲、后段继续转向槽位的三次贝塞尔曲线移动，并在到达后先缩小再恢复。调用前必须由业务结果确认卡牌允许移动。
        /// </summary>
        public Tween AnimateTo(Transform parent, Vector2 targetPosition)
        {
            transform.SetParent(parent, true);
            transform.localScale = Vector3.one;
            canvasGroup.alpha = MahjongViewConfig.NormalAlpha;
            Vector2 startPosition = rectTransform.anchoredPosition;
            Vector2 firstControlPosition = new Vector2(
                startPosition.x,
                Mathf.Lerp(startPosition.y, targetPosition.y, 0.45f));
            Vector2 secondControlPosition = new Vector2(startPosition.x, targetPosition.y);
            return DOTween.Sequence()
                .Append(DOTween.To(
                        () => 0f,
                        progress =>
                        {
                            float inverseProgress = 1f - progress;
                            float inverseProgressSquared = inverseProgress * inverseProgress;
                            float progressSquared = progress * progress;
                            rectTransform.anchoredPosition = inverseProgressSquared * inverseProgress * startPosition +
                                                             3f * inverseProgressSquared * progress * firstControlPosition +
                                                             3f * inverseProgress * progressSquared * secondControlPosition +
                                                             progressSquared * progress * targetPosition;
                        },
                        1f,
                        0.38f)
                    .SetEase(Ease.Linear))
                .Append(transform.DOScale(0.94f, 0.1f))
                .Append(transform.DOScale(1f, 0.1f))
                .SetTarget(this);
        }

        /// <summary>
        /// 将拖拽卡牌设置到槽位父节点，并使用简单世界坐标移动进入目标槽位。
        /// </summary>
        public Tween AnimateDraggedTo(Transform parent, Vector2 targetPosition)
        {
            transform.SetParent(parent, true);
            transform.localScale = Vector3.one;
            canvasGroup.alpha = MahjongViewConfig.NormalAlpha;
            Vector3 targetWorldPosition = parent.TransformPoint(targetPosition);
            return DOTween.Sequence()
                .Append(transform.DOMove(targetWorldPosition, 0.1f))
                .Append(transform.DOScale(0.94f, 0.1f))
                .Append(transform.DOScale(1f, 0.1f))
                .SetEase(Ease.Linear)
                .SetTarget(this);
        }

        /// <summary>
        /// 将槽内卡牌平移到新的排列位置。调用前必须保证卡牌已位于卡槽节点下。
        /// </summary>
        public Tween AnimateSlotReposition(Vector2 targetPosition)
        {
            return rectTransform.DOAnchorPos(targetPosition, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
        }

        /// <summary>
        /// 播放卡牌消除动画。调用前必须由业务结果确认卡牌已经消除。
        /// </summary>
        public Tween AnimateEliminated()
        {
            SetInteractable(false);
            return transform.DOScale(Vector3.zero, MahjongViewConfig.EliminateDuration)
                .SetEase(Ease.InBack)
                .SetTarget(this);
        }

        /// <summary>
        /// 处理开始拖拽。调用前 EventSystem 必须提供有效指针数据。
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isPointerEnabled || !isDragEnabled || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            isDragging = true;
            suppressClick = true;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetParent(dragArea, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 pointerWorldPosition))
            {
                pointerWorldOffset = pointerWorldPosition - rectTransform.position;
            }
        }

        /// <summary>
        /// 处理拖拽移动。调用前必须已通过 OnBeginDrag 开始有效拖拽。
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 pointerWorldPosition))
            {
                rectTransform.position = pointerWorldPosition - pointerWorldOffset;
            }
        }

        /// <summary>
        /// 处理结束拖拽。仅当松手位置位于卡槽区域时派发选择事件，否则返回原位。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            canvasGroup.blocksRaycasts = isPointerEnabled;
            bool releasedInSlot = IsCardCenterInsideSlot();

            if (releasedInSlot)
            {
                selectRequested(this, true);
            }
            else
            {
                AnimateBack();
            }

            DOVirtual.DelayedCall(MahjongViewConfig.ClickRestoreDelay, () => suppressClick = false)
                .SetTarget(this);
        }

        /// <summary>
        /// 处理左键点击并派发选择事件。拖拽手势结束后的合成点击会被忽略。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isPointerEnabled || suppressClick || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            selectRequested(this, false);
        }

        /// <summary>
        /// 判断卡牌中心是否位于卡槽世界矩形内。调用前必须已完成卡槽区域绑定。
        /// </summary>
        private bool IsCardCenterInsideSlot()
        {
            Vector3 slotLocalPosition = slotArea.InverseTransformPoint(rectTransform.position);
            Rect slotRect = slotArea.rect;
            float normalizedX = Mathf.InverseLerp(slotRect.xMin, slotRect.xMax, slotLocalPosition.x);
            float normalizedY = Mathf.InverseLerp(slotRect.yMin, slotRect.yMax, slotLocalPosition.y);
            bool insideX = normalizedX > MahjongViewConfig.NormalizedRectMinimum &&
                           normalizedX < MahjongViewConfig.NormalizedRectMaximum;
            bool insideY = normalizedY > MahjongViewConfig.NormalizedRectMinimum &&
                           normalizedY < MahjongViewConfig.NormalizedRectMaximum;
            return insideX && insideY;
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
        public const float NormalizedRectMinimum = 0f; // 卡槽矩形归一化最小边界
        public const float NormalizedRectMaximum = 1f; // 卡槽矩形归一化最大边界
        public const float NormalAlpha = 1f; // 可操作卡牌透明度
        public const float BlockedAlpha = 1f; // 被阻挡卡牌透明度
        public const float MoveToSlotDuration = 0.25f; // 卡牌移动到卡槽的动画时长
        public const float ReturnDuration = 0.2f; // 卡牌返回牌面的动画时长
        public const float EliminateDuration = 0.2f; // 卡牌消除动画时长
        public const float ClickRestoreDelay = 0.1f; // 拖拽结束后恢复点击的延迟
        public const float BoardCellWidth = 163f; // 牌面完整网格横向间距
        public const float BoardCellHeight = 190f; // 牌面完整网格纵向间距
        public const float LayerVisualOffsetX = 9f; // 每升一层额外增加的向左X轴视觉偏移
        public const float SlotCellWidth = 191f; // 卡槽卡牌横向间距
    }
}
