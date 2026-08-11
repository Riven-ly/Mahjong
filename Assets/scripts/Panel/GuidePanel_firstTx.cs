using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidePanel_firstTx : UIBase
{
    public Button maskBtn; // 全屏引导点击按钮
    public Button tagetBtn; // 目标区域点击按钮
    public Text str; // 引导说明文本
    public Transform mask; // 目标聚焦遮罩
    public Transform trans; // 手势指引节点

    private Button targetBtn;
    private void Start()
    {
        maskBtn.onClick.AddListener(() =>
        {

        });
        tagetBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.GetUI<PlayerInfoUI>().GoldCanvasRecover();
            targetBtn.onClick.Invoke();
            Hide();
            PlayerPrefs.SetString("GuidePanel_firstTx", "yes");
        });
    }
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        PlayerInfoUI playerInfoUI = UIManager.Instance.GetUI<PlayerInfoUI>();
        TxBtn txBtn = playerInfoUI.txTrans.GetChild(0).GetComponent<TxBtn>();
        playerInfoUI.GoldCanvasTop();
        targetBtn = txBtn.btn;
        mask.position = txBtn.btn.transform.position;
        trans.position = txBtn.btn.transform.position;
    }
    public override void Hide()
    {
        base.Hide();
        OtherSdkManager.Instance.CustomEvent("newbie_guide_2_complete", "step", 2);
    }
}
