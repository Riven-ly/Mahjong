using UnityEngine;
using UnityEngine.UI;

public sealed class Task5Slider : MonoBehaviour
{
    public Image progressFill;
    public Text progressText;

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);

    public void Refresh()
    {
        SdkTaskProgress progress = PhaseTaskService.GetSdkProgress(PhaseId.PlayerLevel30);
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(progress.Normalized);
        if (progressText != null)
            progressText.text = FormatProgress(progress);
    }

    static string FormatProgress(SdkTaskProgress progress) => progress.Target <= 0f
        ? string.Empty
        : $"Lv.{progress.Current:0}/Lv.{progress.Target:0}";
}
