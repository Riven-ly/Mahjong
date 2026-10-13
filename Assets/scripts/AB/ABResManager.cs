using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ABResManager : MonoBehaviour
{
    public static ABResManager Instance;

    public Sprite diamondsSprite;
    public Sprite diamondRerardIconsSprite;

    //新的UI界面
    public List<GameObject> uiPanel;

    //提现入口（TaskTip + 提现按钮）
    public TxWithdrawBar txWithdrawBarPrefab;

    //顶部/底部飘条公告（TxHudSliders + TopNotice + BottomNotice）
    public TipMgr txTipsRootPrefab;

    //TMP 文字材质兜底 shader（TextMeshPro/Distance Field）
    public Shader tmpSdfShader;

    public TxWithdrawBar TxWithdrawBar { get; private set; }
    public TipMgr TxTipsRoot { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        UpdateDiamondsUI();
        if (uiPanel != null)
        {
            foreach (var ui in uiPanel)
            {
                UIManager.Instance.AddSpecialUI(ui.gameObject);
            }
        }

        CreateTxWithdrawBar();
        CreateTxTipsRoot();
    }

    void RefreshTMPMaterials(GameObject root)
    {
        var shader = tmpSdfShader != null ? tmpSdfShader : Shader.Find("TextMeshPro/Distance Field");
        if (shader == null)
            return;

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text == null)
                continue;

            var material = text.fontSharedMaterial;
                material.shader = shader;

            var instance = text.fontMaterial;
                instance.shader = shader;

            text.SetMaterialDirty();
            text.ForceMeshUpdate(true);
        }
    }

    void CreateTxWithdrawBar()
    {
        if (txWithdrawBarPrefab == null)
            return;

        PlayerInfoUI playerInfoUI = UIManager.Instance.GetUI<PlayerInfoUI>();
        if (playerInfoUI == null || playerInfoUI.txTrans == null)
            return;

        TxWithdrawBar = Instantiate(txWithdrawBarPrefab, playerInfoUI.txTrans);
        TxWithdrawBar.RefreshTip();
        RefreshTMPMaterials(TxWithdrawBar.gameObject);
    }

    void CreateTxTipsRoot()
    {
        if (txTipsRootPrefab == null)
            return;

        PlayerInfoUI playerInfoUI = UIManager.Instance.GetUI<PlayerInfoUI>();
        if (playerInfoUI == null || playerInfoUI.txTipsRootTrans == null)
            return;

        TxTipsRoot = Instantiate(txTipsRootPrefab, playerInfoUI.txTipsRootTrans);
    }

    private void UpdateDiamondsUI()
    {
        GameManager.Instance.specialDiamonds[1] = diamondsSprite;
        GameManager.Instance.specialRewardsDuis[1] = diamondRerardIconsSprite;
        EventManager.Instance.TriggerEvent(GameEvent.UpdateAppATTUI);
    }
}
