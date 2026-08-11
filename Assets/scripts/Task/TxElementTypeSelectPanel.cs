using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TxElementTypeSelectPanel : UIBase
{
    public Text title;
    public Text explain;
    public InputField inputField1;

    public List<Button> buttons;
    public Transform selectBtnIcon;
    public Button hideBtn;
    public Button submitBtn;
    private int index;
    private void Start()
    {
        hideBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            Hide();
        });

        submitBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayBtnMusic();
            SubmitOnClick();
        });


        for (int i = 0; i < buttons.Count; i++)
        {
            int _index = i;
            buttons[_index].onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayBtnMusic();
                index = _index;
                //UpdateselectBtnIconPos();
            });
            buttons[_index].interactable = false;
        }
        buttons[0].interactable = true;
 
    }
    public override void Refresh(object data = null)
    {
        base.Refresh(data);
        string s1 = LanguageManager.Instance.GetText_Encrypt("Wh");
        title.text = string.Format(LanguageManager.Instance.GetText("TxElementTypeSelectPanel_title"), s1);
        explain.text = LanguageManager.Instance.GetText("TxElementTypeSelectPanel_explain");

        string s2 = LanguageManager.Instance.GetText_Encrypt("wh");
        inputField1.placeholder.transform.GetComponent<Text>().text = string.Format(LanguageManager.Instance.GetText("TxElementTypeSelectPanel_input1"), s2);
        inputField1.text = "";

        index = 0;
        //UpdateselectBtnIconPos();
    }

    public override void Hide()
    {
        base.Hide();
    }

    private void UpdateselectBtnIconPos()
    {
        Vector3 vec = buttons[index].transform.localPosition;
        vec.y += 5f;
        selectBtnIcon.localPosition = vec;
    }

    public void SubmitOnClick()
    {
        if(string.IsNullOrEmpty(inputField1.text))
        {
            string str = LanguageManager.Instance.GetText("TxElementTypeSelectPanel_Error2");
            UIManager.Instance.OpenUI<GeneralTipsPanel>(str);
        }
        else
        {
            if (GameManager.CheckSimpleEmail(inputField1.text))
            {
                TxManager.Instance.saveData.AccountStr = inputField1.text;
                TxManager.Instance.SaveTasks();
                UIManager.Instance.GetUI<TxPanel>().RefreshAccountUI();
                Hide();
            }
            else
            {
                string str = LanguageManager.Instance.GetText("TxElementTypeSelectPanel_Error2");
                UIManager.Instance.OpenUI<GeneralTipsPanel>(str);
            }
        }
    }
}
