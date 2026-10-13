using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class Task4Slider : MonoBehaviour
{
    public Image progressFill;
    public TextMeshProUGUI progressText;

    void Awake()
    {
        if (progressFill == null)
            progressFill = transform.Find("Fill")?.GetComponent<Image>();
        if (progressText == null)
            progressText = transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
    }

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);

    public void Refresh()
    {
        SdkTaskProgress progress = PhaseTaskService.GetSdkProgress(PhaseId.ActiveDays7);
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(progress.Normalized);
        if (progressText != null)
            progressText.text = FormatProgress(progress);
    }

    static string FormatProgress(SdkTaskProgress progress) => progress.Target <= 0f
        ? string.Empty
        : $"Day {progress.Current:0}/{progress.Target:0}";
}
