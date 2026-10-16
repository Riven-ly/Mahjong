using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class Task1Slider : MonoBehaviour
{
    public PhaseOneHudConfigSO config;
    public PhaseLevelNodeView[] levelNodes;
    public GameObject limitLevelDisplay;
    public GameObject percentDisplay;
    public Text percentText;

    readonly List<PhaseSubLevelDot> subDots = new List<PhaseSubLevelDot>();

    public PhaseOneHudConfigSO Config => config;

    void Awake()
    {
        subDots.AddRange(limitLevelDisplay.GetComponentsInChildren<PhaseSubLevelDot>(true));
        percentDisplay.SetActive(false);
    }

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);

    public void Refresh(PhaseOneHudProgress progress, bool phaseCompleted)
    {
        for (int index = 0; index < levelNodes.Length; index++)
        {
            int level = index + 1;
            levelNodes[index].SetState(phaseCompleted || level < progress.DisplayLevel
                ? PhaseLevelNodeState.Complete
                : level == progress.DisplayLevel
                    ? PhaseLevelNodeState.Current
                    : PhaseLevelNodeState.NoComplete);
        }

        bool showSubDots = !phaseCompleted && progress.IsValid && progress.DotCount > 0;
        bool showPercent = showSubDots && progress.ShowPercent;
        limitLevelDisplay.SetActive(showSubDots);
        percentDisplay.SetActive(showPercent);
        if (showPercent)
            percentText.text = PhaseOneHudCalculator.FormatPercent(progress.Percent);
        if (!showSubDots)
            return;

        for (int index = 0; index < subDots.Count; index++)
        {
            PhaseSubLevelDot dot = subDots[index];
            bool visible = index < progress.DotCount;
            dot.gameObject.SetActive(visible);
            if (!visible)
                continue;

            int dotIndex = index + 1;
            dot.Bind(
                dotIndex <= progress.CompletedDots,
                dotIndex == progress.CurrentDot,
                progress.ShowPercent && dotIndex == progress.CurrentDot,
                progress.Percent);
        }
    }
}
