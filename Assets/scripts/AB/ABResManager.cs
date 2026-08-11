using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABResManager : MonoBehaviour
{
    public GameObject txElementManangerPrefab;
    public GameObject TxElementBtnPrefab;
    public GameObject BobaoPrefab;

    public Sprite diamondsSprite;
    public Sprite diamondRerardIconsSprite;

    //新的UI界面
    public List<GameObject> uiPanel;
    // Start is called before the first frame update
    void Start()
    {
        UpdateDiamondsUI();
        InitTxElementPanel();
        if (uiPanel != null)
        {
            foreach (var ui in uiPanel)
            {
                UIManager.Instance.AddSpecialUI(ui.gameObject);
            }
        }

    }
    private void UpdateDiamondsUI()
    {
        GameManager.Instance.specialDiamonds[1] = diamondsSprite;
        GameManager.Instance.specialRewardsDuis[1] = diamondRerardIconsSprite;
        EventManager.Instance.TriggerEvent(GameEvent.UpdateAppATTUI);
    }

    private void InitTxElementPanel()
    {
        Instantiate(txElementManangerPrefab);
        Instantiate(TxElementBtnPrefab, UIManager.Instance.GetUI<PlayerInfoUI>().txTrans);
        Instantiate(BobaoPrefab, UIManager.Instance.GetUI<GameScenePanel>().bobao);
    }
}
