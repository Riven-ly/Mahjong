using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TxBtn : MonoBehaviour,IEventListener
{
    public Button btn;
    public Text btnText;
    public Text ex;
    private void OnEnable()
    {
        EventManager.Instance.RegisterListener(GameEvent.GetGold, this);
    }
    private void OnDisable()
    {
        EventManager.Instance.UnregisterListener(GameEvent.GetGold, this);
    }
    // Start is called before the first frame update
    void Start()
    {
        btn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            AdManager.Instance.OnClickInterstitialAd("");
            UIManager.Instance.OpenUI<TxPanel>();
        });

        btnText.text = LanguageManager.Instance.GetText_Encrypt("WD");
        RefreshUI();
        TxManager.Instance.TasksChanged += RefreshUI;
    }

    public void OnEventTriggered(GameEvent eventType, object data = null)
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        TxTaskData txTaskData = TxManager.Instance.Tasks[0];
        var type = TxManager.Instance.GetTaskStage(txTaskData);
        string WD = LanguageManager.Instance.GetText_Encrypt("wd");
        string unit = LanguageManager.Instance.GetText_Encrypt("Special_Diamond__unit");
        switch (type)
        {
            case TxTaskStage.Amount:
                string t1 = LanguageManager.Instance.GetText("TxBtn_t1");
                float targetV = MathF.Round(txTaskData.amount - GameManager.Instance.playerInfo.Gold, 2);
                targetV = Mathf.Max(targetV, 0f);
                ex.text = string.Format(t1, $"{unit}{targetV}", $"{unit}{txTaskData.amount}", WD);
                break;
            case TxTaskStage.Win:
                string t2 = LanguageManager.Instance.GetText("TxBtn_t2");
                ex.text = string.Format(t2, $"{txTaskData.winTarget- txTaskData.winProgress}", WD);
                break;
            case TxTaskStage.Login:
                string t3 = LanguageManager.Instance.GetText("TxBtn_t3");
                int diffDay = txTaskData.loginTarget - TxManager.Instance.GetLoginDays();
                diffDay = Mathf.Max(diffDay, 0);
                ex.text = string.Format(t3, $"{diffDay}", WD);
                break;
            case TxTaskStage.Completed:
                string t4 = LanguageManager.Instance.GetText("TxBtn_t4");
                ex.text = string.Format(t4, WD);
                break;
        }
    }

  
}
