using System.Globalization;
using UnityEngine;

public readonly struct PhaseOneHudProgress
{
    public int ActualLevel { get; }
    public int DisplayLevel { get; }
    public int DotCount { get; }
    public int CurrentDot { get; }
    public int CompletedDots { get; }
    public bool ShowPercent { get; }
    public float Percent { get; }
    public bool IsValid { get; }

    public PhaseOneHudProgress(
        int actualLevel,
        int displayLevel,
        int dotCount,
        int currentDot,
        int completedDots,
        bool showPercent,
        float percent,
        bool isValid)
    {
        ActualLevel = actualLevel;
        DisplayLevel = displayLevel;
        DotCount = dotCount;
        CurrentDot = currentDot;
        CompletedDots = completedDots;
        ShowPercent = showPercent;
        Percent = percent;
        IsValid = isValid;
    }
}

public static class PhaseOneHudCalculator
{
    public static PhaseOneHudProgress Get(int actualLevel, PhaseOneHudConfigSO config)
    {
        int clampedLevel = Mathf.Clamp(actualLevel, 1, 30);
        if (config == null)
        {
            Debug.LogError("101");
            return CreateFallback(clampedLevel);
        }

        PhaseOneHudEntry entry = config.Get(clampedLevel);
        if (entry == null)
        {
            Debug.LogError("101", config);
            return CreateFallback(clampedLevel);
        }

        return new PhaseOneHudProgress(
            clampedLevel,
            entry.DisplayLevel,
            entry.DotCount,
            entry.CurrentDot,
            entry.CompletedDots,
            entry.ShowPercent,
            Mathf.Clamp01(entry.Percent),
            true);
    }

    public static string FormatPercent(float percent) =>
        (Mathf.Clamp01(percent) * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    static PhaseOneHudProgress CreateFallback(int actualLevel) =>
        new PhaseOneHudProgress(actualLevel, 1, 0, 0, 0, false, 0f, false);
}
