using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TxWithdrawBar : MonoBehaviour
{
    public TextMeshProUGUI taskTipText;
    public TextMeshProUGUI buttonText;
    public Button withdrawButton;

    void Awake()
    {
        RefreshButtonText();
        if (withdrawButton != null)
            withdrawButton.onClick.AddListener(OnWithdrawClick);
    }

    void OnEnable()
    {
        PhaseTaskEvents.ProgressChanged += OnTaskChanged;
        PhaseTaskEvents.PhaseCompleted += OnPhaseChanged;
        PhaseTaskEvents.CurrentPhaseChanged += OnPhaseChanged;
        RefreshTip();
    }

    void OnDisable()
    {
        PhaseTaskEvents.ProgressChanged -= OnTaskChanged;
        PhaseTaskEvents.PhaseCompleted -= OnPhaseChanged;
        PhaseTaskEvents.CurrentPhaseChanged -= OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (withdrawButton != null)
            withdrawButton.onClick.RemoveListener(OnWithdrawClick);
    }

    public void RefreshButtonText()
    {
        if (buttonText == null)
            return;

        buttonText.text = TxGameBridge.GetEncrypted("ch");
    }

    public void RefreshTip()
    {
        if (taskTipText == null)
            return;

        Transform tipRoot = taskTipText.transform.parent;
        if (tipRoot == null)
            return;

        if (PhaseTaskService.IsAllPhaseCompleted)
        {
            tipRoot.gameObject.SetActive(false);
            return;
        }

        tipRoot.gameObject.SetActive(true);
        taskTipText.text = PhaseTaskService.GetCurrentTaskText();
    }

    void OnTaskChanged()
    {
        RefreshTip();
    }

    void OnPhaseChanged(PhaseId phaseId)
    {
        RefreshTip();
    }

    void OnWithdrawClick()
    {
        UIManager.Instance.OpenUI<CandyPanel>();
    }
}
