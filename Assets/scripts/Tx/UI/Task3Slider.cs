using UnityEngine;
using UnityEngine.UI;

public sealed class Task3Slider : MonoBehaviour
{
    public Image progressFill;
    public Text progressText;

    void Awake()
    {
        if (progressFill == null)
            progressFill = transform.Find("Fill")?.GetComponent<Image>();
        if (progressText == null)
            progressText = transform.Find("Text (TMP)")?.GetComponent<Text>();
    }

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);

    public void Refresh()
    {
        SdkTaskProgress progress = PhaseTaskService.GetSdkProgress(PhaseId.Coin2000);
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(progress.Normalized);
        if (progressText != null)
            progressText.text = FormatProgress(progress);
    }

    static string FormatProgress(SdkTaskProgress progress) => progress.Target <= 0f
        ? string.Empty
        : TxGameBridge.GetMark() + progress.Current.ToString("0.##") + "/" + TxGameBridge.GetMark() + progress.Target.ToString("0.##");
}
