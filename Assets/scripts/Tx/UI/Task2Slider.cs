using UnityEngine;
using UnityEngine.UI;

public sealed class Task2Slider : MonoBehaviour
{
    public Image totalProgress;
    public Text slotText;

    bool invalidProgressLogged;

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);

    public void Refresh(ActiveMinuteProgress progress)
    {
        if (!progress.IsValid)
        {
            totalProgress.fillAmount = 0f;
            slotText.text = string.Empty;
            if (!invalidProgressLogged)
            {
                invalidProgressLogged = true;
                Debug.LogError("[Tx] 分钟进度无效");
            }
            return;
        }

        invalidProgressLogged = false;
        totalProgress.fillAmount = Mathf.Clamp01(progress.TotalNormalized);
        slotText.text = $"{progress.SlotIndex}/{progress.SlotCount}";
    }
}
