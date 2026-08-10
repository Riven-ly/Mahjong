using MahjongGame.View;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首关首次消除的手势引导数据。
/// </summary>
public sealed class FirstGameGuideData
{
    public readonly MahjongGameplayView gameplayView; // 当前麻将玩法视图
    public readonly int firstTargetCardId; // 第一个引导目标卡牌实例ID
    public readonly RectTransform firstTargetRectTransform; // 第一个引导目标卡牌矩形变换
    public readonly int secondTargetCardId; // 第二个引导目标卡牌实例ID
    public readonly RectTransform secondTargetRectTransform; // 第二个引导目标卡牌矩形变换

    /// <summary>
    /// 创建首关引导所需的两张目标牌数据。
    /// </summary>
    public FirstGameGuideData(
        MahjongGameplayView gameplayView,
        int firstTargetCardId,
        RectTransform firstTargetRectTransform,
        int secondTargetCardId,
        RectTransform secondTargetRectTransform)
    {
        this.gameplayView = gameplayView;
        this.firstTargetCardId = firstTargetCardId;
        this.firstTargetRectTransform = firstTargetRectTransform;
        this.secondTargetCardId = secondTargetCardId;
        this.secondTargetRectTransform = secondTargetRectTransform;
    }
}

/// <summary>
/// 首关首次消除的手势引导面板。
/// </summary>
public class GuidePanel_firstGame : UIBase
{
    public Button maskBtn; // 全屏引导点击按钮
    public Button tagetBtn; // 目标区域点击按钮
    public Text str; // 引导说明文本
    public Transform mask; // 目标聚焦遮罩
    public Transform trans; // 手势指引节点

    private FirstGameGuideData guideData; // 当前引导目标数据
    private bool isGuidingSecondCard; // 是否正在引导第二张牌

    /// <summary>
    /// 注册引导点击事件。
    /// </summary>
    private void Start()
    {
        tagetBtn.onClick.AddListener(() =>
        {
            SelectGuideCard();
        });
    }

    /// <summary>
    /// 根据首张目标牌位置刷新首关引导表现。
    /// </summary>
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        guideData = data as FirstGameGuideData;
        if (guideData == null)
        {
            Hide();
            return;
        }

        isGuidingSecondCard = false;
        UpdateGuidePosition(guideData.firstTargetRectTransform);
    }

    /// <summary>
    /// 转发当前目标牌点击，并在第一张完成后切换至第二张。
    /// </summary>
    private void SelectGuideCard()
    {
        if (!isGuidingSecondCard)
        {
            guideData.gameplayView.SelectFirstGameGuideCard(guideData.firstTargetCardId);
            isGuidingSecondCard = true;
            UpdateGuidePosition(guideData.secondTargetRectTransform);
            return;
        }

        guideData.gameplayView.SelectFirstGameGuideCard(guideData.secondTargetCardId);
        PlayerPrefs.SetInt("FirstGameGuideCompleted", 1);
        PlayerPrefs.Save();
        Hide();
    }

    /// <summary>
    /// 将手势与聚焦遮罩移动至指定目标牌。
    /// </summary>
    private void UpdateGuidePosition(RectTransform targetRectTransform)
    {
        Vector3 targetPosition = targetRectTransform.position;
        mask.position = targetPosition;
        trans.position = targetPosition;
    }

    /// <summary>
    /// 关闭引导面板并清除当前目标数据。
    /// </summary>
    public override void Hide()
    {
        guideData = null;
        isGuidingSecondCard = false;
        base.Hide();
    }
}
