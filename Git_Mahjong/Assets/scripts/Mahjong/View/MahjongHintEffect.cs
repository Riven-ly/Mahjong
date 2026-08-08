using DG.Tweening;
using UnityEngine;

namespace MahjongGame.View
{
    /// <summary>
    /// 提示特效显示期间驱动所属麻将卡牌呼吸缩放的组件。
    /// </summary>
    public sealed class MahjongHintEffect : MonoBehaviour
    {
        [SerializeField] private MahjongCell mahjongCell; // 所属麻将卡牌视图

        /// <summary>
        /// 提示特效启用时开始卡牌循环呼吸缩放。
        /// </summary>
        private void OnEnable()
        {
            mahjongCell.transform.localScale = Vector3.one;
            mahjongCell.transform.DOScale(1.06f, 0.55f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        /// <summary>
        /// 提示特效关闭时停止呼吸缩放并恢复卡牌原始尺寸。
        /// </summary>
        private void OnDisable()
        {
            DOTween.Kill(this);
            if (mahjongCell != null)
            {
                mahjongCell.transform.localScale = Vector3.one;
            }
        }
    }
}
